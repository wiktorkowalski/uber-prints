using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.DTOs;

public class UpdatePrintRequestDto
{
  [Required]
  [MaxLength(100)]
  public string RequesterName { get; set; } = string.Empty;

  [Required]
  [Url]
  [MaxLength(500)]
  public string ModelUrl { get; set; } = string.Empty;

  [MaxLength(1000)]
  public string? Notes { get; set; }

  public bool RequestDelivery { get; set; }

  public bool IsPublic { get; set; }

  [Required]
  public Guid FilamentId { get; set; }
}
