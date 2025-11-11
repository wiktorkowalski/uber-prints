using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;
using UberPrints.Server.Services;

namespace UberPrints.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // TODO: Add [Authorize(Roles = "Admin")] when admin authorization is implemented
public class PrintersController : ControllerBase
{
  private readonly ApplicationDbContext _context;
  private readonly PrusaLinkClient _prusaLink;
  private readonly ILogger<PrintersController> _logger;

  public PrintersController(
    ApplicationDbContext context,
    PrusaLinkClient prusaLink,
    ILogger<PrintersController> logger)
  {
    _context = context;
    _prusaLink = prusaLink;
    _logger = logger;
  }

  [HttpGet]
  [AllowAnonymous]
  public async Task<IActionResult> GetPrinter()
  {
    var printer = await _context.Printers.FirstOrDefaultAsync();

    if (printer == null)
    {
      return NotFound(new { Message = "No printer configured" });
    }

    return Ok(MapToPrinterDto(printer));
  }

  [HttpPost("test-connection")]
  [AllowAnonymous]
  public async Task<IActionResult> TestConnection()
  {
    var printer = await _context.Printers.FirstOrDefaultAsync();

    if (printer == null)
    {
      return NotFound(new { Message = "No printer configured" });
    }

    _prusaLink.ConfigureForPrinter(printer.IpAddress, printer.ApiKey);
    var connected = await _prusaLink.TestConnectionAsync();

    if (connected)
    {
      var version = await _prusaLink.GetVersionAsync();
      return Ok(new
      {
        Connected = true,
        Version = version
      });
    }

    return Ok(new { Connected = false });
  }

  [HttpPost("upload")]
  public async Task<IActionResult> UploadGCode(IFormFile file, [FromQuery] bool startPrint = false)
  {
    var printer = await _context.Printers.FirstOrDefaultAsync();

    if (printer == null)
    {
      return NotFound(new { Message = "No printer configured" });
    }

    if (file == null || file.Length == 0)
    {
      return BadRequest("No file provided");
    }

    _prusaLink.ConfigureForPrinter(printer.IpAddress, printer.ApiKey);

    using var stream = file.OpenReadStream();
    var success = await _prusaLink.UploadFileAsync(stream, file.FileName, printAfterUpload: startPrint);

    if (success)
    {
      _logger.LogInformation("Uploaded file {FileName} to printer", file.FileName);
      return Ok(new { Success = true, FileName = file.FileName });
    }

    return StatusCode(500, new { Success = false, Message = "Failed to upload file" });
  }

  [HttpPost("pause")]
  public async Task<IActionResult> PausePrint()
  {
    var printer = await _context.Printers.FirstOrDefaultAsync();

    if (printer == null)
    {
      return NotFound(new { Message = "No printer configured" });
    }

    _prusaLink.ConfigureForPrinter(printer.IpAddress, printer.ApiKey);
    var success = await _prusaLink.PausePrintAsync();

    return Ok(new { Success = success });
  }

  [HttpPost("resume")]
  public async Task<IActionResult> ResumePrint()
  {
    var printer = await _context.Printers.FirstOrDefaultAsync();

    if (printer == null)
    {
      return NotFound(new { Message = "No printer configured" });
    }

    _prusaLink.ConfigureForPrinter(printer.IpAddress, printer.ApiKey);
    var success = await _prusaLink.ResumePrintAsync();

    return Ok(new { Success = success });
  }

  [HttpPost("cancel")]
  public async Task<IActionResult> CancelPrint()
  {
    var printer = await _context.Printers.FirstOrDefaultAsync();

    if (printer == null)
    {
      return NotFound(new { Message = "No printer configured" });
    }

    _prusaLink.ConfigureForPrinter(printer.IpAddress, printer.ApiKey);
    var success = await _prusaLink.CancelPrintAsync();

    return Ok(new { Success = success });
  }

  [HttpGet("snapshot")]
  [AllowAnonymous]
  public async Task<IActionResult> GetSnapshot()
  {
    var printer = await _context.Printers.FirstOrDefaultAsync();

    if (printer == null)
    {
      return NotFound(new { Message = "No printer configured" });
    }

    _prusaLink.ConfigureForPrinter(printer.IpAddress, printer.ApiKey);
    var snapshot = await _prusaLink.GetSnapshotAsync();

    if (snapshot == null)
    {
      return NotFound("No camera snapshot available");
    }

    return File(snapshot, "image/jpeg");
  }

  private static PrinterDto MapToPrinterDto(Printer printer)
  {
    return new PrinterDto
    {
      Id = printer.Id,
      Name = printer.Name,
      IpAddress = printer.IpAddress,
      IsActive = printer.IsActive,
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
      CurrentFileName = printer.CurrentFileName,
      AxisX = printer.AxisX,
      AxisY = printer.AxisY,
      AxisZ = printer.AxisZ,
      FlowRate = printer.FlowRate,
      SpeedRate = printer.SpeedRate,
      FanHotend = printer.FanHotend,
      FanPrint = printer.FanPrint,
      CreatedAt = printer.CreatedAt,
      UpdatedAt = printer.UpdatedAt
    };
  }
}
