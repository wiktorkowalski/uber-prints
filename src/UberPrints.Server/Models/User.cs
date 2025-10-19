using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Models;

public class User
{
  [Key]
  public Guid Id { get; set; }

  [MaxLength(100)]
  public string? DiscordId { get; set; }

  [MaxLength(255)]
  public string? GuestSessionToken { get; set; }

  [Required]
  [MaxLength(100)]
  public string Username { get; set; } = string.Empty;

  [MaxLength(100)]
  public string? GlobalName { get; set; }

  [MaxLength(255)]
  public string? AvatarHash { get; set; }

  public bool IsAdmin { get; set; }

  public DateTime CreatedAt { get; set; }

  public List<PrintRequest> PrintRequests { get; set; } = new();
}
