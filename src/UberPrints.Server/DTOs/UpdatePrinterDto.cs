using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.DTOs;

public class UpdatePrinterDto
{
  [MaxLength(100)]
  public string? Name { get; set; }

  [MaxLength(100)]
  [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$|^[a-zA-Z0-9.-]+$", ErrorMessage = "Must be a valid IP address or hostname")]
  public string? IpAddress { get; set; }

  [MaxLength(255)]
  public string? ApiKey { get; set; }

  [MaxLength(200)]
  public string? Location { get; set; }

  public bool? IsActive { get; set; }
}
