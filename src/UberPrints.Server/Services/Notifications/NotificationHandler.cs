using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Data;

namespace UberPrints.Server.Services.Notifications;

// DiscordService and ThermalPrinterService log their own outcomes, so this class only logs a missing request
internal sealed class NotificationHandler(
  ApplicationDbContext context,
  DiscordService discordService,
  ThermalPrinterService thermalPrinterService,
  ILogger<NotificationHandler> logger) : INotificationHandler
{
  public Task HandleAsync(NotificationMessage message, CancellationToken cancellationToken) => message switch
  {
    NewRequestNotification newRequest => HandleNewRequestAsync(newRequest, cancellationToken),
    StatusChangedNotification statusChanged => HandleStatusChangedAsync(statusChanged, cancellationToken),
    _ => throw new NotSupportedException($"Unknown notification type {message.GetType().Name}")
  };

  private async Task HandleNewRequestAsync(NewRequestNotification message, CancellationToken cancellationToken)
  {
    // The ticket prints the filament name
    var request = await context.PrintRequests
      .Include(r => r.Filament)
      .FirstOrDefaultAsync(r => r.Id == message.RequestId, cancellationToken);
    if (request is null)
    {
      logger.LogWarning("Skipped new-request notification because request {RequestId} no longer exists", message.RequestId);
      return;
    }

    await discordService.NotifyAdminsNewRequestAsync(request, cancellationToken);
    await thermalPrinterService.PrintNewRequestAsync(request, cancellationToken);
  }

  private async Task HandleStatusChangedAsync(StatusChangedNotification message, CancellationToken cancellationToken)
  {
    // The DM goes to User.DiscordId
    var request = await context.PrintRequests
      .Include(r => r.User)
      .FirstOrDefaultAsync(r => r.Id == message.RequestId, cancellationToken);
    if (request is null)
    {
      logger.LogWarning("Skipped status-change notification because request {RequestId} no longer exists", message.RequestId);
      return;
    }

    await discordService.NotifyRequesterStatusChangeAsync(request, message.OldStatus, message.NewStatus, cancellationToken);
  }
}
