using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.DTOs;

public class CreatePrinterDto
{
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  [Required]
  [MaxLength(100)]
  [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$|^[a-zA-Z0-9.-]+$", ErrorMessage = "Must be a valid IP address or hostname")]
  public string IpAddress { get; set; } = string.Empty;

  [Required]
  [MaxLength(255)]
  public string ApiKey { get; set; } = string.Empty;

  [MaxLength(200)]
  public string? Location { get; set; }

  public bool IsActive { get; set; } = true;
}
