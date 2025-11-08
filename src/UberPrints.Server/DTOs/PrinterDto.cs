using UberPrints.Server.Models;

namespace UberPrints.Server.DTOs;

public class PrinterDto
{
  public Guid Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string IpAddress { get; set; } = string.Empty;
  public bool IsActive { get; set; }
  public string? Location { get; set; }
  public PrinterStateEnum CurrentState { get; set; }
  public DateTime? LastStatusUpdate { get; set; }
  public double? NozzleTemperature { get; set; }
  public double? NozzleTargetTemperature { get; set; }
  public double? BedTemperature { get; set; }
  public double? BedTargetTemperature { get; set; }
  public int? PrintProgress { get; set; }
  public int? TimeRemaining { get; set; }
  public int? TimePrinting { get; set; }
  public string? CurrentFileName { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
