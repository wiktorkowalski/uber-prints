using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Models;

public class PrinterStatusHistory
{
  [Key]
  public Guid Id { get; set; }

  [Required]
  public Guid PrinterId { get; set; }

  [Required]
  public PrinterStateEnum State { get; set; }

  public double? NozzleTemperature { get; set; }

  public double? BedTemperature { get; set; }

  public int? PrintProgress { get; set; }

  [MaxLength(500)]
  public string? FileName { get; set; }

  public DateTime Timestamp { get; set; }

  public Printer Printer { get; set; } = null!;
}
