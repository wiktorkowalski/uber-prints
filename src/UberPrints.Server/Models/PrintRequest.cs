using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Models;

public class PrintRequest
{
  [Key]
  public Guid Id { get; set; }

  public Guid? UserId { get; set; }

  [MaxLength(255)]
  public string? GuestTrackingToken { get; set; }

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

  public bool IsPublic { get; set; } = true;

  public bool NotifyOnStatusChange { get; set; } = true;

  public Guid? FilamentId { get; set; }

  public Guid? AssignedPrinterId { get; set; }

  [MaxLength(500)]
  public string? GCodeFileUrl { get; set; }

  [MaxLength(255)]
  public string? PrintJobId { get; set; }

  public DateTime? PrintStartedAt { get; set; }

  public DateTime? PrintCompletedAt { get; set; }

  [Required]
  public RequestStatusEnum CurrentStatus { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public User? User { get; set; }

  public Filament? Filament { get; set; }

  public Printer? AssignedPrinter { get; set; }

  public List<StatusHistory> StatusHistory { get; set; } = new();

  public List<PrintRequestChange> Changes { get; set; } = new();
}
