using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;

namespace UberPrints.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
  private readonly ApplicationDbContext _context;
  private readonly IConfiguration _configuration;

  public AuthController(ApplicationDbContext context, IConfiguration configuration)
  {
    _context = context;
    _configuration = configuration;
  }

  [HttpGet("login")]
  public IActionResult Login([FromQuery] string? returnUrl = null, [FromQuery] string? guestSessionToken = null)
  {
    // Store guest session token in temp data if provided
    if (!string.IsNullOrEmpty(guestSessionToken))
    {
      HttpContext.Session.SetString("GuestSessionToken", guestSessionToken);
    }

    var properties = new AuthenticationProperties
    {
      RedirectUri = Url.Action(nameof(DiscordCallback), new { returnUrl })
    };

    return Challenge(properties, "Discord");
  }

  [HttpGet("discord/callback")]
  public async Task<IActionResult> DiscordCallback([FromQuery] string? returnUrl = null)
  {
    var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    if (!result.Succeeded)
    {
      return BadRequest("Authentication failed");
    }

    var discordId = result.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var username = result.Principal?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

    // Fetch additional user data from Discord API using the access token
    string? globalName = null;
    string? avatarHash = null;

    var accessToken = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "access_token");

    if (!string.IsNullOrEmpty(accessToken))
    {
      try
      {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var response = await httpClient.GetAsync("https://discord.com/api/users/@me");

        if (response.IsSuccessStatusCode)
        {
          var userData = await response.Content.ReadFromJsonAsync<DiscordUserResponse>();
          if (userData != null)
          {
            globalName = userData.GlobalName;
            avatarHash = userData.Avatar;
          }
        }
      }
      catch
      {
        // If we fail to fetch additional data, we'll continue with what we have from claims
      }
    }

    if (string.IsNullOrEmpty(discordId))
    {
      return BadRequest("Failed to retrieve Discord user information");
    }

    // Check if we have a guest session token to link
    string? guestSessionToken = null;
    if (HttpContext.Session.Keys.Contains("GuestSessionToken"))
    {
      guestSessionToken = HttpContext.Session.GetString("GuestSessionToken");
      HttpContext.Session.Remove("GuestSessionToken");
    }

    // Find or create user
    var user = await _context.Users
        .Include(u => u.PrintRequests)
        .FirstOrDefaultAsync(u => u.DiscordId == discordId);

    if (user == null)
    {
      // Check if there's a guest user with this session token to convert
      if (!string.IsNullOrEmpty(guestSessionToken))
      {
        user = await _context.Users
            .Include(u => u.PrintRequests)
            .FirstOrDefaultAsync(u => u.GuestSessionToken == guestSessionToken);

        if (user != null)
        {
          // Convert guest to authenticated user
          user.DiscordId = discordId;
          user.Username = username;
          user.GlobalName = globalName;
          user.AvatarHash = avatarHash;
          // Keep the GuestSessionToken for continuity
        }
      }

      // Create new user if still null
      if (user == null)
      {
        user = new User
        {
          DiscordId = discordId,
          Username = username,
          GlobalName = globalName,
          AvatarHash = avatarHash,
          IsAdmin = false,
          CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
      }

      await _context.SaveChangesAsync();
    }
    else
    {
      // Update existing user info
      user.Username = username;
      user.GlobalName = globalName;
      user.AvatarHash = avatarHash;

      // If guest session token provided, link any guest requests
      if (!string.IsNullOrEmpty(guestSessionToken))
      {
        // Find guest user - could be a pure guest (no DiscordId) or this same user from a previous session
        var guestUser = await _context.Users
            .Include(u => u.PrintRequests)
            .FirstOrDefaultAsync(u => u.GuestSessionToken == guestSessionToken);

        // Only transfer if it's a different user (pure guest account)
        if (guestUser != null && guestUser.Id != user.Id && guestUser.DiscordId == null)
        {
          // Transfer guest requests to authenticated user
          foreach (var request in guestUser.PrintRequests)
          {
            request.UserId = user.Id;
          }

          // Optionally delete the now-empty guest account
          _context.Users.Remove(guestUser);

          await _context.SaveChangesAsync();
        }
      }

      await _context.SaveChangesAsync();
    }

    // Generate JWT token
    var token = GenerateJwtToken(user);

    // Store user info in cookie claims
    var claims = new List<Claim>
    {
      new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
      new Claim(ClaimTypes.Name, user.Username),
      new Claim("IsAdmin", user.IsAdmin.ToString())
    };

    if (user.IsAdmin)
    {
      claims.Add(new Claim(ClaimTypes.Role, "Admin"));
    }

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var authProperties = new AuthenticationProperties
    {
      IsPersistent = true,
      ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
    };

    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(claimsIdentity),
        authProperties);

    // Redirect to frontend auth callback with token
    // Use the request's origin (scheme + host) to support both local dev and production
    var origin = $"{Request.Scheme}://{Request.Host}";
    var redirectUrl = $"{origin}/auth/callback?token={token}";

    // Append returnUrl if provided
    if (!string.IsNullOrEmpty(returnUrl))
    {
      redirectUrl += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    return Redirect(redirectUrl);
  }

  [HttpGet("me")]
  [Authorize]
  public async Task<IActionResult> GetCurrentUser()
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

    var userDto = new UserDto
    {
      Id = user.Id,
      DiscordId = user.DiscordId,
      Username = user.Username,
      GlobalName = user.GlobalName,
      AvatarHash = user.AvatarHash,
      IsAdmin = user.IsAdmin,
      CreatedAt = user.CreatedAt
    };

    return Ok(userDto);
  }

  [HttpPost("logout")]
  public async Task<IActionResult> Logout()
  {
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Ok(new { message = "Logged out successfully" });
  }

  [HttpPost("guest")]
  public async Task<IActionResult> CreateGuestSession()
  {
    // Generate a unique guest session token
    var guestSessionToken = GenerateGuestSessionToken();

    // Create guest user
    var guestUser = new User
    {
      GuestSessionToken = guestSessionToken,
      Username = $"Guest_{guestSessionToken.Substring(0, 8)}",
      IsAdmin = false,
      CreatedAt = DateTime.UtcNow
    };

    _context.Users.Add(guestUser);
    await _context.SaveChangesAsync();

    return Ok(new { guestSessionToken, userId = guestUser.Id });
  }

  [HttpPost("refresh")]
  [Authorize]
  public async Task<IActionResult> RefreshToken()
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

    // Generate a new JWT token
    var newToken = GenerateJwtToken(user);

    return Ok(new { token = newToken });
  }

  private string GenerateJwtToken(User user)
  {
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
        _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey not configured")));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
      new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
      new Claim(ClaimTypes.Name, user.Username),
      new Claim("IsAdmin", user.IsAdmin.ToString())
    };

    if (user.IsAdmin)
    {
      claims.Add(new Claim(ClaimTypes.Role, "Admin"));
    }

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"] ?? "UberPrints",
        audience: _configuration["Jwt:Audience"] ?? "UberPrints",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(double.Parse(_configuration["Jwt:ExpiryHours"] ?? "1")),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private string GenerateGuestSessionToken()
  {
    return Guid.NewGuid().ToString("N").ToUpper();
  }

  // DTO for Discord API user response
  private class DiscordUserResponse
  {
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("discriminator")]
    public string? Discriminator { get; set; }

    [JsonPropertyName("global_name")]
    public string? GlobalName { get; set; }
  }
}
