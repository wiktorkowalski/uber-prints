using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Data;
using UberPrints.Server.Models;
using UberPrints.Server.DTOs;
using UberPrints.Server.Services;

namespace UberPrints.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
  private readonly ApplicationDbContext _context;
  private readonly IChangeTrackingService _changeTrackingService;

  public RequestsController(ApplicationDbContext context, IChangeTrackingService changeTrackingService)
  {
    _context = context;
    _changeTrackingService = changeTrackingService;
  }

  [HttpGet]
  public async Task<IActionResult> GetRequests()
  {
    // Get current user ID if authenticated
    Guid? currentUserId = null;
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
    {
      currentUserId = userId;
    }
    else if (Request.Headers.TryGetValue("X-Guest-Session-Token", out var guestTokenHeader))
    {
      var guestToken = guestTokenHeader.ToString();
      if (!string.IsNullOrEmpty(guestToken))
      {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.GuestSessionToken == guestToken);
        if (user != null)
        {
          currentUserId = user.Id;
        }
      }
    }

    var requests = await _context.PrintRequests
        .Include(r => r.Filament)
        .Include(r => r.User)
        .Include(r => r.StatusHistory)
            .ThenInclude(sh => sh.ChangedByUser)
        .Where(r => r.IsPublic || r.UserId == currentUserId)
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
        .Include(r => r.Changes)
            .ThenInclude(c => c.ChangedByUser)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (request == null)
    {
      return NotFound();
    }

    // Check privacy: allow if public or user owns the request
    if (!request.IsPublic)
    {
      // Get current user ID if authenticated
      Guid? currentUserId = null;
      var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
      if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
      {
        currentUserId = userId;
      }
      else if (Request.Headers.TryGetValue("X-Guest-Session-Token", out var guestTokenHeader))
      {
        var guestToken = guestTokenHeader.ToString();
        if (!string.IsNullOrEmpty(guestToken))
        {
          var user = await _context.Users.FirstOrDefaultAsync(u => u.GuestSessionToken == guestToken);
          if (user != null)
          {
            currentUserId = user.Id;
          }
        }
      }

      // If request is private and user doesn't own it, return NotFound (not Forbidden to avoid info leak)
      if (request.UserId != currentUserId)
      {
        return NotFound();
      }
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
        .Include(r => r.Changes)
            .ThenInclude(c => c.ChangedByUser)
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
    // Validate filament exists if provided
    if (dto.FilamentId.HasValue)
    {
      var filament = await _context.Filaments.FindAsync(dto.FilamentId.Value);
      if (filament == null)
      {
        return BadRequest("Invalid filament selected.");
      }
    }

    // Allow requests without filament - admin will assign one later
    // Also allow requests with out-of-stock or pending filaments

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
      IsPublic = dto.IsPublic,
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

    // Validate ownership - user must own the request to update it
    // Check for authenticated user via JWT
    Guid? currentUserId = null;
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
    {
      currentUserId = userId;
      if (request.UserId != userId)
      {
        return Forbid(); // 403 Forbidden - authenticated but not authorized
      }
    }
    else
    {
      // Check for guest session token in header
      if (Request.Headers.TryGetValue("X-Guest-Session-Token", out var guestTokenHeader))
      {
        var guestToken = guestTokenHeader.ToString();
        if (!string.IsNullOrEmpty(guestToken))
        {
          var user = await _context.Users.FirstOrDefaultAsync(u => u.GuestSessionToken == guestToken);
          if (user == null || request.UserId != user.Id)
          {
            return Forbid(); // Guest session exists but doesn't own this request
          }
          currentUserId = user.Id;
          // Guest owns the request, continue
        }
        else
        {
          return Unauthorized(); // No valid session
        }
      }
      else
      {
        return Unauthorized(); // No authentication provided
      }
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

    // Allow requests without filament - admin will assign one later
    // Also allow requests with out-of-stock or pending filaments

    // Create a snapshot of the old request for change tracking
    var oldRequest = new PrintRequest
    {
      Id = request.Id,
      RequesterName = request.RequesterName,
      ModelUrl = request.ModelUrl,
      Notes = request.Notes,
      RequestDelivery = request.RequestDelivery,
      IsPublic = request.IsPublic,
      FilamentId = request.FilamentId
    };

    // Update the request
    request.RequesterName = dto.RequesterName;
    request.ModelUrl = dto.ModelUrl;
    request.Notes = dto.Notes;
    request.RequestDelivery = dto.RequestDelivery;
    request.IsPublic = dto.IsPublic;
    request.FilamentId = dto.FilamentId;
    request.UpdatedAt = DateTime.UtcNow;

    // Track changes
    await _changeTrackingService.TrackChangesAsync(oldRequest, request, currentUserId);

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

    // Validate ownership - user must own the request to delete it
    // Check for authenticated user via JWT
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
    {
      if (request.UserId != userId)
      {
        return Forbid(); // 403 Forbidden - authenticated but not authorized
      }
    }
    else
    {
      // Check for guest session token in header
      if (Request.Headers.TryGetValue("X-Guest-Session-Token", out var guestTokenHeader))
      {
        var guestToken = guestTokenHeader.ToString();
        if (!string.IsNullOrEmpty(guestToken))
        {
          var user = await _context.Users.FirstOrDefaultAsync(u => u.GuestSessionToken == guestToken);
          if (user == null || request.UserId != user.Id)
          {
            return Forbid(); // Guest session exists but doesn't own this request
          }
          // Guest owns the request, continue
        }
        else
        {
          return Unauthorized(); // No valid session
        }
      }
      else
      {
        return Unauthorized(); // No authentication provided
      }
    }

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
      }).ToList(),
      Changes = request.Changes.Select(c => new PrintRequestChangeDto
      {
        Id = c.Id,
        PrintRequestId = c.PrintRequestId,
        FieldName = c.FieldName,
        OldValue = c.OldValue,
        NewValue = c.NewValue,
        ChangedByUserId = c.ChangedByUserId,
        ChangedByUsername = c.ChangedByUser?.Username,
        ChangedAt = c.ChangedAt
      }).OrderByDescending(c => c.ChangedAt).ToList()
    };
  }

  private string GenerateTrackingToken()
  {
    // Generate a unique tracking token
    return Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();
  }
}
