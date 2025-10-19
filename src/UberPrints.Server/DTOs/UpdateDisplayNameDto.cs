using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.DTOs;

public class UpdateDisplayNameDto
{
  [Required]
  [MaxLength(100)]
  public string DisplayName { get; set; } = string.Empty;
}
