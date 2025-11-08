using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;

namespace UberPrints.Server.Controllers;

[ApiController]
[Route("api/printer")]
public class PrinterStatusController : ControllerBase
{
  private readonly ApplicationDbContext _context;

  public PrinterStatusController(ApplicationDbContext context)
  {
    _context = context;
  }

  /// <summary>
  /// Get current status of the first active printer (public endpoint)
  /// </summary>
  [HttpGet("status")]
  public async Task<IActionResult> GetPrinterStatus()
  {
    var printer = await _context.Printers
      .Where(p => p.IsActive)
      .OrderBy(p => p.Name)
      .FirstOrDefaultAsync();

    if (printer == null)
    {
      return NotFound(new { Message = "No active printers found" });
    }

    return Ok(MapToStatusDto(printer));
  }

  /// <summary>
  /// Get all active printers status (public endpoint)
  /// </summary>
  [HttpGet("status/all")]
  public async Task<IActionResult> GetAllPrintersStatus()
  {
    var printers = await _context.Printers
      .Where(p => p.IsActive)
      .OrderBy(p => p.Name)
      .ToListAsync();

    var dtos = printers.Select(MapToStatusDto).ToList();
    return Ok(dtos);
  }

  /// <summary>
  /// Get current print queue (public endpoint)
  /// </summary>
  [HttpGet("queue")]
  public async Task<IActionResult> GetPrintQueue()
  {
    var queuedRequests = await _context.PrintRequests
      .Include(r => r.User)
      .Include(r => r.Filament)
      .Where(r => r.CurrentStatus == RequestStatusEnum.Accepted && r.PrintStartedAt == null)
      .OrderBy(r => r.UpdatedAt)
      .Take(10)
      .ToListAsync();

    var queue = queuedRequests.Select(r => new
    {
      r.Id,
      r.RequesterName,
      r.ModelUrl,
      FilamentName = r.Filament != null ? $"{r.Filament.Brand} {r.Filament.Colour}" : null,
      AcceptedAt = r.UpdatedAt
    }).ToList();

    return Ok(queue);
  }

  private static PrinterStatusDto MapToStatusDto(Printer printer)
  {
    return new PrinterStatusDto
    {
      Id = printer.Id,
      Name = printer.Name,
      Location = printer.Location,
      CurrentState = printer.CurrentState,
      LastStatusUpdate = printer.LastStatusUpdate,
      NozzleTemperature = printer.NozzleTemperature,
      NozzleTargetTemperature = printer.NozzleTargetTemperature,
      BedTemperature = printer.BedTemperature,
      BedTargetTemperature = printer.BedTargetTemperature,
      PrintProgress = printer.PrintProgress,
      TimeRemaining = printer.TimeRemaining,
      TimePrinting = printer.TimePrinting,
      CurrentFileName = printer.CurrentFileName
    };
  }
}
