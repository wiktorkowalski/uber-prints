namespace UberPrints.Server.DTOs;

public class UserDto
{
  public Guid Id { get; set; }
  public string? DiscordId { get; set; }
  public string Username { get; set; } = string.Empty;
  public string? Email { get; set; }
  public bool IsAdmin { get; set; }
  public DateTime CreatedAt { get; set; }
}
