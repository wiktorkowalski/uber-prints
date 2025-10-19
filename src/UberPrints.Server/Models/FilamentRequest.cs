using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Models;

public class FilamentRequest
{
  [Key]
  public Guid Id { get; set; }

  public Guid? UserId { get; set; }

  [Required]
  [MaxLength(100)]
  public string RequesterName { get; set; } = string.Empty;

  [Required]
  [MaxLength(50)]
  public string Material { get; set; } = string.Empty;

  [Required]
  [MaxLength(100)]
  public string Brand { get; set; } = string.Empty;

  [Required]
  [MaxLength(50)]
  public string Colour { get; set; } = string.Empty;

  [Url]
  [MaxLength(500)]
  public string? Link { get; set; }

  [MaxLength(1000)]
  public string? Notes { get; set; }

  [Required]
  public FilamentRequestStatusEnum CurrentStatus { get; set; }

  public Guid? FilamentId { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public User? User { get; set; }

  public Filament? Filament { get; set; }

  public List<FilamentRequestStatusHistory> StatusHistory { get; set; } = new();
}
