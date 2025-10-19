using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Models;

public class FilamentRequestStatusHistory
{
  [Key]
  public Guid Id { get; set; }

  [Required]
  public Guid FilamentRequestId { get; set; }

  [Required]
  public FilamentRequestStatusEnum Status { get; set; }

  [MaxLength(500)]
  public string? Reason { get; set; }

  public Guid? ChangedByUserId { get; set; }

  public DateTime CreatedAt { get; set; }

  public FilamentRequest FilamentRequest { get; set; } = null!;

  public User? ChangedByUser { get; set; }
}
