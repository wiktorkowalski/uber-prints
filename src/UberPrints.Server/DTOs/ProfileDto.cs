namespace UberPrints.Server.DTOs;

public class ProfileDto
{
  public Guid Id { get; set; }
  public string? DiscordId { get; set; }
  public string Username { get; set; } = string.Empty;
  public string? GlobalName { get; set; }
  public string? AvatarHash { get; set; }
  public string? AvatarUrl { get; set; }
  public bool IsAdmin { get; set; }
  public DateTime CreatedAt { get; set; }
}
