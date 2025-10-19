using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Data;
using UberPrints.Server.Models;
using UberPrints.Server.DTOs;

namespace UberPrints.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilamentRequestsController : ControllerBase
{
  private readonly ApplicationDbContext _context;

  public FilamentRequestsController(ApplicationDbContext context)
  {
    _context = context;
  }

  [HttpGet]
  public async Task<IActionResult> GetFilamentRequests()
  {
    var requests = await _context.FilamentRequests
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
  public async Task<IActionResult> GetFilamentRequest(Guid id)
  {
    var request = await _context.FilamentRequests
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

  [HttpGet("my-requests")]
  public async Task<IActionResult> GetMyFilamentRequests()
  {
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

    if (user == null)
    {
      return Unauthorized();
    }

    var requests = await _context.FilamentRequests
        .Include(r => r.Filament)
        .Include(r => r.User)
        .Include(r => r.StatusHistory)
            .ThenInclude(sh => sh.ChangedByUser)
        .Where(r => r.UserId == user.Id)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    var dtos = requests.Select(MapToDto).ToList();
    return Ok(dtos);
  }

  [HttpPost]
  public async Task<IActionResult> CreateFilamentRequest(CreateFilamentRequestDto dto)
  {
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

    var request = new FilamentRequest
    {
      RequesterName = dto.RequesterName,
      Material = dto.Material,
      Brand = dto.Brand,
      Colour = dto.Colour,
      Link = dto.Link,
      Notes = dto.Notes,
      UserId = user.Id,
      CurrentStatus = FilamentRequestStatusEnum.Pending,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.FilamentRequests.Add(request);

    // Add initial status history
    var statusHistory = new FilamentRequestStatusHistory
    {
      FilamentRequestId = request.Id,
      Status = FilamentRequestStatusEnum.Pending,
      CreatedAt = DateTime.UtcNow
    };
    request.StatusHistory.Add(statusHistory);

    await _context.SaveChangesAsync();

    var responseDto = MapToDto(request);
    return CreatedAtAction(nameof(GetFilamentRequest), new { id = request.Id }, responseDto);
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteFilamentRequest(Guid id)
  {
    var request = await _context.FilamentRequests.FindAsync(id);
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

    _context.FilamentRequests.Remove(request);
    await _context.SaveChangesAsync();

    return NoContent();
  }

  private FilamentRequestDto MapToDto(FilamentRequest request)
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
