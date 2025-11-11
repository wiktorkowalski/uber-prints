using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UberPrints.Server.Configuration;
using UberPrints.Server.Data;
using UberPrints.Server.Models;

namespace UberPrints.Server.Services;

/// <summary>
/// Background service that monitors printer status via PrusaLink API
/// </summary>
public class PrinterMonitoringService : BackgroundService
{
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<PrinterMonitoringService> _logger;
  private readonly PrusaLinkOptions _options;

  public PrinterMonitoringService(
    IServiceProvider serviceProvider,
    ILogger<PrinterMonitoringService> logger,
    IOptions<PrusaLinkOptions> options)
  {
    _serviceProvider = serviceProvider;
    _logger = logger;
    _options = options.Value;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("PrinterMonitoringService starting");

    // Wait a bit before starting to allow app to fully initialize
    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

    // Ensure default printer exists
    await EnsureDefaultPrinterExistsAsync(stoppingToken);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await MonitorPrinterAsync(stoppingToken);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in printer monitoring loop");
      }

      // Determine next poll interval based on printer state
      var interval = await GetNextPollIntervalAsync(stoppingToken);
      await Task.Delay(interval, stoppingToken);
    }

    _logger.LogInformation("PrinterMonitoringService stopping");
  }

  private async Task EnsureDefaultPrinterExistsAsync(CancellationToken ct)
  {
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var existingPrinter = await dbContext.Printers.FirstOrDefaultAsync(ct);

    if (existingPrinter == null)
    {
      var printer = new Printer
      {
        Name = "Default Printer",
        IpAddress = _options.IpAddress,
        ApiKey = _options.ApiKey,
        IsActive = true,
        CurrentState = PrinterStateEnum.Unknown,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      dbContext.Printers.Add(printer);
      await dbContext.SaveChangesAsync(ct);

      _logger.LogInformation("Created default printer with IP {IpAddress}", _options.IpAddress);
    }
    else
    {
      // Update IP and API key from config if changed
      if (existingPrinter.IpAddress != _options.IpAddress || existingPrinter.ApiKey != _options.ApiKey)
      {
        existingPrinter.IpAddress = _options.IpAddress;
        existingPrinter.ApiKey = _options.ApiKey;
        existingPrinter.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Updated printer configuration from appsettings");
      }
    }
  }

  private async Task MonitorPrinterAsync(CancellationToken ct)
  {
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var prusaLink = scope.ServiceProvider.GetRequiredService<PrusaLinkClient>();

    var printer = await dbContext.Printers.FirstOrDefaultAsync(ct);

    if (printer == null)
    {
      _logger.LogWarning("No printer found in database");
      return;
    }

    try
    {
      await UpdatePrinterStatusAsync(printer, prusaLink, dbContext, ct);
      await dbContext.SaveChangesAsync(ct);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to update printer status");
      printer.CurrentState = PrinterStateEnum.Error;
      await dbContext.SaveChangesAsync(ct);
    }
  }

  private async Task UpdatePrinterStatusAsync(
    Printer printer,
    PrusaLinkClient prusaLink,
    ApplicationDbContext dbContext,
    CancellationToken ct)
  {
    // Configure client for this printer
    prusaLink.ConfigureForPrinter(printer.IpAddress, printer.ApiKey);

    // Get status from API
    var status = await prusaLink.GetStatusAsync(ct);
    if (status == null)
    {
      _logger.LogWarning("Failed to get status for printer {PrinterName}", printer.Name);
      printer.CurrentState = PrinterStateEnum.Unknown;
      return;
    }

    var previousState = printer.CurrentState;

    // Update printer state
    if (status.Printer?.State != null && Enum.TryParse<PrinterStateEnum>(status.Printer.State, true, out var state))
    {
      printer.CurrentState = state;
    }

    // Update temperatures
    printer.NozzleTemperature = status.Printer?.TempNozzle;
    printer.NozzleTargetTemperature = status.Printer?.TargetNozzle;
    printer.BedTemperature = status.Printer?.TempBed;
    printer.BedTargetTemperature = status.Printer?.TargetBed;

    // Update job information
    printer.PrintProgress = (int?)(status.Job?.Progress ?? 0);
    printer.TimeRemaining = status.Job?.TimeRemaining;
    printer.TimePrinting = status.Job?.TimePrinting;
    printer.CurrentFileName = status.Job?.File?.DisplayName ?? status.Job?.File?.Name;

    // Update axis positions
    printer.AxisX = status.Printer?.AxisX;
    printer.AxisY = status.Printer?.AxisY;
    printer.AxisZ = status.Printer?.AxisZ;

    // Update flow and speed rates
    printer.FlowRate = status.Printer?.Flow;
    printer.SpeedRate = status.Printer?.Speed;

    // Update fan speeds
    printer.FanHotend = status.Printer?.FanHotend;
    printer.FanPrint = status.Printer?.FanPrint;

    printer.LastStatusUpdate = DateTime.UtcNow;
    printer.UpdatedAt = DateTime.UtcNow;

    // Create status history entry if state changed
    if (previousState != printer.CurrentState || ShouldRecordSnapshot(printer))
    {
      var historyEntry = new PrinterStatusHistory
      {
        PrinterId = printer.Id,
        State = printer.CurrentState,
        NozzleTemperature = printer.NozzleTemperature,
        BedTemperature = printer.BedTemperature,
        PrintProgress = printer.PrintProgress,
        FileName = printer.CurrentFileName,
        Timestamp = DateTime.UtcNow
      };

      dbContext.PrinterStatusHistories.Add(historyEntry);

      _logger.LogInformation(
        "Printer {PrinterName} state changed: {PreviousState} -> {NewState}",
        printer.Name, previousState, printer.CurrentState);
    }
  }

  private bool ShouldRecordSnapshot(Printer printer)
  {
    // Record periodic snapshots for active prints
    if (printer.CurrentState == PrinterStateEnum.Printing)
    {
      // Record every 10% progress change or every 5 minutes (whichever comes first)
      return true; // Simplified - in production, add logic to check last snapshot time
    }

    return false;
  }

  private async Task<TimeSpan> GetNextPollIntervalAsync(CancellationToken ct)
  {
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var printer = await dbContext.Printers.FirstOrDefaultAsync(ct);

    var isPrinting = printer?.CurrentState == PrinterStateEnum.Printing;

    return isPrinting
      ? TimeSpan.FromSeconds(_options.PollingIntervalActive)
      : TimeSpan.FromSeconds(_options.PollingIntervalIdle);
  }
}
