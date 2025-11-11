using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Models;

public class Printer
{
  [Key]
  public Guid Id { get; set; }

  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  [Required]
  [MaxLength(100)]
  public string IpAddress { get; set; } = string.Empty;

  [Required]
  [MaxLength(255)]
  public string ApiKey { get; set; } = string.Empty;

  public bool IsActive { get; set; } = true;

  [MaxLength(200)]
  public string? Location { get; set; }

  public PrinterStateEnum CurrentState { get; set; } = PrinterStateEnum.Unknown;

  public DateTime? LastStatusUpdate { get; set; }

  public Guid? CurrentJobId { get; set; }

  [MaxLength(2000)]
  public string? Capabilities { get; set; }

  public double? NozzleTemperature { get; set; }

  public double? NozzleTargetTemperature { get; set; }

  public double? BedTemperature { get; set; }

  public double? BedTargetTemperature { get; set; }

  public int? PrintProgress { get; set; }

  public int? TimeRemaining { get; set; }

  public int? TimePrinting { get; set; }

  [MaxLength(500)]
  public string? CurrentFileName { get; set; }

  public double? AxisX { get; set; }

  public double? AxisY { get; set; }

  public double? AxisZ { get; set; }

  public int? FlowRate { get; set; }

  public int? SpeedRate { get; set; }

  public int? FanHotend { get; set; }

  public int? FanPrint { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public List<PrintRequest> PrintRequests { get; set; } = new();

  public List<PrinterStatusHistory> StatusHistory { get; set; } = new();
}
