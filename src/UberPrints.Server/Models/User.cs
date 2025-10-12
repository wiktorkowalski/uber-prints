using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Models;

public class User
{
  [Key]
  public Guid Id { get; set; }

  [MaxLength(100)]
  public string? DiscordId { get; set; }

  [Required]
  [MaxLength(100)]
  public string Username { get; set; } = string.Empty;

  [EmailAddress]
  [MaxLength(255)]
  public string? Email { get; set; }

  public bool IsAdmin { get; set; }

  public DateTime CreatedAt { get; set; }

  public List<PrintRequest> PrintRequests { get; set; } = new();
}
