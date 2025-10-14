using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Data;
using UberPrints.Server.Models;
using UberPrints.Server.DTOs;

namespace UberPrints.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
  private readonly ApplicationDbContext _context;

  public RequestsController(ApplicationDbContext context)
  {
    _context = context;
  }

  [HttpGet]
  public async Task<IActionResult> GetRequests()
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

  [HttpGet("{id}")]
  public async Task<IActionResult> GetRequest(Guid id)
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

    var dto = MapToDto(request);
    return Ok(dto);
  }

  [HttpGet("track/{token}")]
  public async Task<IActionResult> TrackRequest(string token)
  {
    var request = await _context.PrintRequests
        .Include(r => r.Filament)
        .Include(r => r.User)
        .Include(r => r.StatusHistory)
            .ThenInclude(sh => sh.ChangedByUser)
        .FirstOrDefaultAsync(r => r.GuestTrackingToken == token);

    if (request == null)
    {
      return NotFound();
    }

    var dto = MapToDto(request);
    return Ok(dto);
  }

  [HttpPost]
  public async Task<IActionResult> CreateRequest(CreatePrintRequestDto dto)
  {
    // Validate filament exists and has stock
    var filament = await _context.Filaments.FindAsync(dto.FilamentId);
    if (filament == null)
    {
      return BadRequest("Invalid filament selected.");
    }

    if (filament.StockAmount <= 0)
    {
      return BadRequest("Selected filament is out of stock.");
    }

    // Determine the user (authenticated or guest)
    User? user = null;

    // Check if user is authenticated via JWT
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
    {
      user = await _context.Users.FindAsync(userId);
    }

    // If not authenticated, check for guest session token in header
    if (user == null && Request.Headers.TryGetValue("X-Guest-Session-Token", out var guestTokenHeader))
    {
      var guestToken = guestTokenHeader.ToString();
      if (!string.IsNullOrEmpty(guestToken))
      {
        user = await _context.Users.FirstOrDefaultAsync(u => u.GuestSessionToken == guestToken);
      }
    }

    // If still no user found, require guest session
    if (user == null)
    {
      return BadRequest("No user session found. Please create a guest session first.");
    }

    var request = new PrintRequest
    {
      RequesterName = dto.RequesterName,
      ModelUrl = dto.ModelUrl,
      Notes = dto.Notes,
      RequestDelivery = dto.RequestDelivery,
      FilamentId = dto.FilamentId,
      UserId = user.Id,  // Associate request with user
      CurrentStatus = RequestStatusEnum.Pending,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      GuestTrackingToken = GenerateTrackingToken()
    };

    _context.PrintRequests.Add(request);

    // Add initial status history
    var statusHistory = new StatusHistory
    {
      Status = RequestStatusEnum.Pending,
      Timestamp = DateTime.UtcNow
    };
    request.StatusHistory.Add(statusHistory);

    await _context.SaveChangesAsync();

    var responseDto = MapToDto(request);
    return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, responseDto);
  }

  [HttpPut("{id}")]
  public async Task<IActionResult> UpdateRequest(Guid id, UpdatePrintRequestDto dto)
  {
    var request = await _context.PrintRequests.FindAsync(id);
    if (request == null)
    {
      return NotFound();
    }

    // For now, allow updates without authentication check
    // TODO: Add authentication and ownership validation

    // Validate filament exists and has stock
    var filament = await _context.Filaments.FindAsync(dto.FilamentId);
    if (filament == null)
    {
      return BadRequest("Invalid filament selected.");
    }

    if (filament.StockAmount <= 0)
    {
      return BadRequest("Selected filament is out of stock.");
    }

    request.RequesterName = dto.RequesterName;
    request.ModelUrl = dto.ModelUrl;
    request.Notes = dto.Notes;
    request.RequestDelivery = dto.RequestDelivery;
    request.FilamentId = dto.FilamentId;
    request.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    var responseDto = MapToDto(request);
    return Ok(responseDto);
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteRequest(Guid id)
  {
    var request = await _context.PrintRequests.FindAsync(id);
    if (request == null)
    {
      return NotFound();
    }

    // For now, allow deletion without authentication check
    // TODO: Add authentication and ownership validation

    _context.PrintRequests.Remove(request);
    await _context.SaveChangesAsync();

    return NoContent();
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
      FilamentId = request.FilamentId,
      FilamentName = request.Filament?.Name ?? string.Empty,
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

  private string GenerateTrackingToken()
  {
    // Generate a unique tracking token
    return Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();
  }
}
