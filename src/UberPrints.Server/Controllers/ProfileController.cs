using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;

namespace UberPrints.Server.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
  private readonly ApplicationDbContext _context;

  public ProfileController(ApplicationDbContext context)
  {
    _context = context;
  }

  [HttpGet]
  public async Task<IActionResult> GetProfile()
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
      return Unauthorized();
    }

    var user = await _context.Users.FindAsync(userId);

    if (user == null)
    {
      return NotFound();
    }

    var profileDto = new ProfileDto
    {
      Id = user.Id,
      DiscordId = user.DiscordId,
      Username = user.Username,
      GlobalName = user.GlobalName,
      AvatarHash = user.AvatarHash,
      AvatarUrl = GetAvatarUrl(user.DiscordId, user.AvatarHash),
      IsAdmin = user.IsAdmin,
      CreatedAt = user.CreatedAt
    };

    return Ok(profileDto);
  }

  [HttpPut("display-name")]
  public async Task<IActionResult> UpdateDisplayName([FromBody] UpdateDisplayNameDto dto)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
      return Unauthorized();
    }

    var user = await _context.Users.FindAsync(userId);

    if (user == null)
    {
      return NotFound();
    }

    // Update the GlobalName field which serves as the display name in the system
    user.GlobalName = dto.DisplayName;
    await _context.SaveChangesAsync();

    var profileDto = new ProfileDto
    {
      Id = user.Id,
      DiscordId = user.DiscordId,
      Username = user.Username,
      GlobalName = user.GlobalName,
      AvatarHash = user.AvatarHash,
      AvatarUrl = GetAvatarUrl(user.DiscordId, user.AvatarHash),
      IsAdmin = user.IsAdmin,
      CreatedAt = user.CreatedAt
    };

    return Ok(profileDto);
  }

  private string? GetAvatarUrl(string? discordId, string? avatarHash)
  {
    if (string.IsNullOrEmpty(discordId) || string.IsNullOrEmpty(avatarHash))
    {
      return null;
    }

    return $"https://cdn.discordapp.com/avatars/{discordId}/{avatarHash}.png";
  }
}
