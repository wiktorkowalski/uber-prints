using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.DTOs;

public class CreateFilamentRequestDto
{
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
}
