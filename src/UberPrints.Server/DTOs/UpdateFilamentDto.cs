using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.DTOs;

public class UpdateFilamentDto
{
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  [Required]
  [MaxLength(50)]
  public string Material { get; set; } = string.Empty;

  [Required]
  [MaxLength(100)]
  public string Brand { get; set; } = string.Empty;

  [Required]
  [MaxLength(50)]
  public string Colour { get; set; } = string.Empty;

  [Range(0, double.MaxValue)]
  public decimal StockAmount { get; set; }

  [MaxLength(20)]
  public string StockUnit { get; set; } = "grams";

  [Url]
  [MaxLength(500)]
  public string? Link { get; set; }

  [Url]
  [MaxLength(500)]
  public string? PhotoUrl { get; set; }
}
