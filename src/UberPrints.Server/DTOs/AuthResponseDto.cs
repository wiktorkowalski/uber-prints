namespace UberPrints.Server.DTOs;

public class AuthResponseDto
{
  public UserDto User { get; set; } = null!;
  public string Token { get; set; } = string.Empty;
  public string? GuestSessionToken { get; set; }
}
