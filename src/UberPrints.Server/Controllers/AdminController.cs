using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;

namespace UberPrints.Server.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
  private readonly ApplicationDbContext _context;

  public AdminController(ApplicationDbContext context)
  {
    _context = context;
  }

  [HttpGet("requests")]
  public async Task<IActionResult> GetAllRequests()
  {
    var requests = await _context.PrintRequests
        .Include(r => r.Filament)
        .Include(r => r.User)
        .Include(r => r.StatusHistory)
            .ThenInclude(sh => sh.ChangedByUser)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    var dtos = requests.Select(MapToDto).ToList();
    return Ok(dtos);
  }

  [HttpPut("requests/{id}/status")]
  public async Task<IActionResult> ChangeRequestStatus(Guid id, ChangeStatusDto dto)
  {
    var request = await _context.PrintRequests
        .Include(r => r.StatusHistory)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (request == null)
    {
      return NotFound();
    }

    // For now, allow status changes without authentication check
    // TODO: Add admin authentication check

    request.CurrentStatus = dto.Status;
    request.UpdatedAt = DateTime.UtcNow;

    // Add status history entry
    var statusHistory = new StatusHistory
    {
      RequestId = request.Id,
      Status = dto.Status,
      AdminNotes = dto.AdminNotes,
      Timestamp = DateTime.UtcNow
      // ChangedByUserId will be set when authentication is added
    };
    _context.StatusHistories.Add(statusHistory);

    await _context.SaveChangesAsync();

    var responseDto = MapToDto(request);
    return Ok(responseDto);
  }

  [HttpPut("requests/{id}")]
  public async Task<IActionResult> UpdateRequest(Guid id, UpdatePrintRequestAdminDto dto)
  {
    var request = await _context.PrintRequests
        .Include(r => r.Filament)
        .Include(r => r.User)
        .Include(r => r.StatusHistory)
            .ThenInclude(sh => sh.ChangedByUser)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (request == null)
    {
      return NotFound();
    }

    // Validate filament exists if provided
    if (dto.FilamentId.HasValue)
    {
      var filament = await _context.Filaments.FindAsync(dto.FilamentId.Value);
      if (filament == null)
      {
        return BadRequest("Invalid filament selected.");
      }
    }

    // Update request fields
    request.RequesterName = dto.RequesterName;
    request.ModelUrl = dto.ModelUrl;
    request.Notes = dto.Notes;
    request.RequestDelivery = dto.RequestDelivery;
    request.IsPublic = dto.IsPublic;
    request.FilamentId = dto.FilamentId;
    request.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    var responseDto = MapToDto(request);
    return Ok(responseDto);
  }

  [HttpPost("filaments")]
  public async Task<IActionResult> CreateFilament(CreateFilamentDto dto)
  {
    // For now, allow creation without authentication check
    // TODO: Add admin authentication check

    var filament = new Filament
    {
      Name = dto.Name,
      Material = dto.Material,
      Brand = dto.Brand,
      Colour = dto.Colour,
      StockAmount = dto.StockAmount,
      StockUnit = dto.StockUnit,
      Link = dto.Link,
      PhotoUrl = dto.PhotoUrl,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Filaments.Add(filament);
    await _context.SaveChangesAsync();

    var responseDto = new FilamentDto
    {
      Id = filament.Id,
      Name = filament.Name,
      Material = filament.Material,
      Brand = filament.Brand,
      Colour = filament.Colour,
      StockAmount = filament.StockAmount,
      StockUnit = filament.StockUnit,
      Link = filament.Link,
      PhotoUrl = filament.PhotoUrl,
      IsAvailable = filament.IsAvailable,
      CreatedAt = filament.CreatedAt,
      UpdatedAt = filament.UpdatedAt
    };

    return CreatedAtAction(nameof(GetFilament), new { id = filament.Id }, responseDto);
  }

  [HttpPut("filaments/{id}")]
  public async Task<IActionResult> UpdateFilament(Guid id, UpdateFilamentDto dto)
  {
    var filament = await _context.Filaments.FindAsync(id);
    if (filament == null)
    {
      return NotFound();
    }

    // For now, allow update without authentication check
    // TODO: Add admin authentication check

    filament.Name = dto.Name;
    filament.Material = dto.Material;
    filament.Brand = dto.Brand;
    filament.Colour = dto.Colour;
    filament.StockAmount = dto.StockAmount;
    filament.StockUnit = dto.StockUnit;
    filament.Link = dto.Link;
    filament.PhotoUrl = dto.PhotoUrl;
    filament.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    var responseDto = new FilamentDto
    {
      Id = filament.Id,
      Name = filament.Name,
      Material = filament.Material,
      Brand = filament.Brand,
      Colour = filament.Colour,
      StockAmount = filament.StockAmount,
      StockUnit = filament.StockUnit,
      Link = filament.Link,
      PhotoUrl = filament.PhotoUrl,
      IsAvailable = filament.IsAvailable,
      CreatedAt = filament.CreatedAt,
      UpdatedAt = filament.UpdatedAt
    };

    return Ok(responseDto);
  }

  [HttpPatch("filaments/{id}/stock")]
  public async Task<IActionResult> UpdateFilamentStock(Guid id, UpdateStockDto dto)
  {
    var filament = await _context.Filaments.FindAsync(id);
    if (filament == null)
    {
      return NotFound();
    }

    // For now, allow update without authentication check
    // TODO: Add admin authentication check

    filament.StockAmount = dto.StockAmount;
    filament.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    var responseDto = new FilamentDto
    {
      Id = filament.Id,
      Name = filament.Name,
      Material = filament.Material,
      Brand = filament.Brand,
      Colour = filament.Colour,
      StockAmount = filament.StockAmount,
      StockUnit = filament.StockUnit,
      Link = filament.Link,
      PhotoUrl = filament.PhotoUrl,
      IsAvailable = filament.IsAvailable,
      CreatedAt = filament.CreatedAt,
      UpdatedAt = filament.UpdatedAt
    };

    return Ok(responseDto);
  }

  [HttpDelete("filaments/{id}")]
  public async Task<IActionResult> DeleteFilament(Guid id)
  {
    var filament = await _context.Filaments.FindAsync(id);
    if (filament == null)
    {
      return NotFound();
    }

    // For now, allow deletion without authentication check
    // TODO: Add admin authentication check

    // Check if filament has active requests
    var hasActiveRequests = await _context.PrintRequests
        .AnyAsync(r => r.FilamentId == id);

    if (hasActiveRequests)
    {
      return BadRequest("Cannot delete filament with active requests.");
    }

    _context.Filaments.Remove(filament);
    await _context.SaveChangesAsync();

    return NoContent();
  }

  [HttpGet("filaments/{id}")]
  public async Task<IActionResult> GetFilament(Guid id)
  {
    var filament = await _context.Filaments.FindAsync(id);

    if (filament == null)
    {
      return NotFound();
    }

    var dto = new FilamentDto
    {
      Id = filament.Id,
      Name = filament.Name,
      Material = filament.Material,
      Brand = filament.Brand,
      Colour = filament.Colour,
      StockAmount = filament.StockAmount,
      StockUnit = filament.StockUnit,
      Link = filament.Link,
      PhotoUrl = filament.PhotoUrl,
      IsAvailable = filament.IsAvailable,
      CreatedAt = filament.CreatedAt,
      UpdatedAt = filament.UpdatedAt
    };

    return Ok(dto);
  }

  [HttpGet("filament-requests")]
  public async Task<IActionResult> GetAllFilamentRequests()
  {
    var requests = await _context.FilamentRequests
        .Include(r => r.Filament)
        .Include(r => r.User)
        .Include(r => r.StatusHistory)
            .ThenInclude(sh => sh.ChangedByUser)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    var dtos = requests.Select(MapFilamentRequestToDto).ToList();
    return Ok(dtos);
  }

  [HttpPut("filament-requests/{id}/status")]
  public async Task<IActionResult> ChangeFilamentRequestStatus(Guid id, ChangeFilamentRequestStatusDto dto)
  {
    var request = await _context.FilamentRequests
        .Include(r => r.StatusHistory)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (request == null)
    {
      return NotFound();
    }

    // Get admin user ID from claims
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    Guid? adminUserId = null;
    if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
    {
      adminUserId = userId;
    }

    request.CurrentStatus = dto.Status;
    request.UpdatedAt = DateTime.UtcNow;

    // If approved and filament is being created/linked
    if (dto.Status == FilamentRequestStatusEnum.Approved && dto.FilamentId.HasValue)
    {
      request.FilamentId = dto.FilamentId.Value;
    }

    // Add status history entry
    var statusHistory = new FilamentRequestStatusHistory
    {
      FilamentRequestId = request.Id,
      Status = dto.Status,
      Reason = dto.Reason,
      ChangedByUserId = adminUserId,
      CreatedAt = DateTime.UtcNow
    };
    _context.FilamentRequestStatusHistories.Add(statusHistory);

    await _context.SaveChangesAsync();

    // Reload with all includes for complete response
    var updatedRequest = await _context.FilamentRequests
        .Include(r => r.Filament)
        .Include(r => r.User)
        .Include(r => r.StatusHistory)
            .ThenInclude(sh => sh.ChangedByUser)
        .FirstOrDefaultAsync(r => r.Id == id);

    var responseDto = MapFilamentRequestToDto(updatedRequest!);
    return Ok(responseDto);
  }

  private PrintRequestDto MapToDto(PrintRequest request)
  {
    return new PrintRequestDto
    {
      Id = request.Id,
      UserId = request.UserId,
      GuestTrackingToken = request.GuestTrackingToken,
      RequesterName = request.RequesterName,
      ModelUrl = request.ModelUrl,
      Notes = request.Notes,
      RequestDelivery = request.RequestDelivery,
      IsPublic = request.IsPublic,
      FilamentId = request.FilamentId,
      FilamentName = request.Filament?.Name,
      CurrentStatus = request.CurrentStatus,
      CreatedAt = request.CreatedAt,
      UpdatedAt = request.UpdatedAt,
      StatusHistory = request.StatusHistory.Select(sh => new StatusHistoryDto
      {
        Id = sh.Id,
        RequestId = sh.RequestId,
        Status = sh.Status,
        ChangedByUserId = sh.ChangedByUserId,
        ChangedByUsername = sh.ChangedByUser?.Username,
        AdminNotes = sh.AdminNotes,
        Timestamp = sh.Timestamp
      }).ToList()
    };
  }

  private FilamentRequestDto MapFilamentRequestToDto(FilamentRequest request)
  {
    return new FilamentRequestDto
    {
      Id = request.Id,
      UserId = request.UserId,
      RequesterName = request.RequesterName,
      Material = request.Material,
      Brand = request.Brand,
      Colour = request.Colour,
      Link = request.Link,
      Notes = request.Notes,
      CurrentStatus = request.CurrentStatus,
      FilamentId = request.FilamentId,
      FilamentName = request.Filament?.Name,
      CreatedAt = request.CreatedAt,
      UpdatedAt = request.UpdatedAt,
      StatusHistory = request.StatusHistory.Select(sh => new FilamentRequestStatusHistoryDto
      {
        Id = sh.Id,
        Status = sh.Status,
        Reason = sh.Reason,
        ChangedByUserId = sh.ChangedByUserId,
        ChangedByUsername = sh.ChangedByUser?.Username,
        CreatedAt = sh.CreatedAt
      }).ToList()
    };
  }
}
