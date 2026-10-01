using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UberPrints.Server.Configuration;
using UberPrints.Server.Data;
using UberPrints.Server.Services;
using UberPrints.Server.Services.Notifications;

namespace UberPrints.Server.UnitTests;

public sealed class NotificationHandlerTests
{
  private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

  [Fact]
  public async Task HandleAsync_CancellingTokenAbortsInFlightThermalPost()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    await using var context = new ApplicationDbContext(options);
    var filament = TestDataFactory.CreateTestFilament();
    var request = TestDataFactory.CreateTestPrintRequest(filamentId: filament.Id);
    context.Filaments.Add(filament);
    context.PrintRequests.Add(request);
    await context.SaveChangesAsync();

    var printerHttp = new HangingHttpHandler();
    var configuration = new ConfigurationBuilder().Build();
    // No bot token, so DiscordService skips and the thermal POST is the only HTTP call
    var discordService = new DiscordService(
      new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
      configuration,
      NullLogger<DiscordService>.Instance,
      new HttpClient(new HangingHttpHandler()));
    var thermalPrinterService = new ThermalPrinterService(
      configuration,
      NullLogger<ThermalPrinterService>.Instance,
      new HttpClient(printerHttp),
      Options.Create(new ThermalPrinterOptions { ApiUrl = "https://printer.invalid/api/Printer" }));
    var handler = new NotificationHandler(context, discordService, thermalPrinterService, NullLogger<NotificationHandler>.Instance);
    using var cts = new CancellationTokenSource();

    var handleTask = handler.HandleAsync(new NewRequestNotification(request.Id), cts.Token);
    await printerHttp.Entered.WaitAsync(WaitTimeout);
    await cts.CancelAsync();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handleTask.WaitAsync(WaitTimeout));
  }
}
