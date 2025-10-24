namespace UberPrints.Server.DTOs;

public class AdminUserDto
{
  public Guid Id { get; set; }
  public string? DiscordId { get; set; }
  public string? GuestSessionToken { get; set; }
  public string Username { get; set; } = string.Empty;
  public string? GlobalName { get; set; }
  public string? AvatarHash { get; set; }
  public bool IsAdmin { get; set; }
  public DateTime CreatedAt { get; set; }
  public int PrintRequestCount { get; set; }
  public int FilamentRequestCount { get; set; }
  public bool IsGuest { get; set; }
}
