using UberPrints.Server.Models;

namespace UberPrints.Server.DTOs;

/// <summary>
/// Public-facing printer status DTO (hides sensitive info like API key and IP)
/// </summary>
public class PrinterStatusDto
{
  public Guid Id { get; set; }
  public string Name { get; set; } = string.Empty;
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
  public double? AxisX { get; set; }
  public double? AxisY { get; set; }
  public double? AxisZ { get; set; }
  public int? FlowRate { get; set; }
  public int? SpeedRate { get; set; }
  public int? FanHotend { get; set; }
  public int? FanPrint { get; set; }
  public bool IsAvailable => CurrentState == PrinterStateEnum.Idle || CurrentState == PrinterStateEnum.Ready;
}
