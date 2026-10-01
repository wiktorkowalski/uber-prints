namespace UberPrints.Server.Services.Notifications;

internal sealed class NotificationWorker(
  NotificationQueue queue,
  IServiceScopeFactory scopeFactory,
  ILogger<NotificationWorker> logger,
  TimeSpan? drainTimeout = null) : BackgroundService
{
  // Docker and Watchtower send SIGKILL 10 s after SIGTERM by default, so the drain must end before that
  internal static readonly TimeSpan DefaultDrainTimeout = TimeSpan.FromSeconds(8);

  private readonly TimeSpan _drainTimeout = drainTimeout ?? DefaultDrainTimeout;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    // Ends when StopAsync completes the queue and it is empty, or when a drain timeout cancels stoppingToken
    // ReadAllAsync keeps yielding buffered items after cancellation, so check the token before each read
    while (await queue.Reader.WaitToReadAsync(stoppingToken))
      while (!stoppingToken.IsCancellationRequested && queue.Reader.TryRead(out var message))
        await ProcessAsync(message, stoppingToken);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    queue.Complete();

    if (ExecuteTask is { } executeTask)
    {
      // WhenAny never throws, so a host-forced stop only ends the wait early
      var finished = await Task.WhenAny(executeTask, Task.Delay(_drainTimeout, cancellationToken));
      if (finished != executeTask)
        logger.LogWarning(
          "Notification drain timed out after {DrainTimeout}; dropping {DroppedCount} queued notifications",
          _drainTimeout, queue.Reader.Count);
    }

    // Cancels stoppingToken after a drain timeout; that stops the item's DB reload, but Discord/printer HTTP calls take no token and run until SIGKILL
    await base.StopAsync(cancellationToken);
  }

  private async Task ProcessAsync(NotificationMessage message, CancellationToken stoppingToken)
  {
    var notificationType = message.GetType().Name;
    using var logScope = logger.BeginScope(new Dictionary<string, object>
    {
      ["RequestId"] = message.RequestId,
      ["NotificationType"] = notificationType
    });

    try
    {
      // The HTTP request scope is gone by now, so each item gets its own DbContext
      await using var scope = scopeFactory.CreateAsyncScope();
      var handler = scope.ServiceProvider.GetRequiredService<INotificationHandler>();
      await handler.HandleAsync(message, stoppingToken);
      logger.LogDebug("Processed {NotificationType} for request {RequestId}", notificationType, message.RequestId);
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
      logger.LogWarning(
        "Aborted {NotificationType} for request {RequestId} because the drain timed out",
        notificationType, message.RequestId);
    }
    catch (Exception ex)
    {
      // One bad item must not stop the loop or crash the host
      logger.LogError(ex, "Failed to process {NotificationType} for request {RequestId}", notificationType, message.RequestId);
    }
  }
}
