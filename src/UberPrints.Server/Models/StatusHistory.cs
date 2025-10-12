using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Models;

public class StatusHistory
{
  [Key]
  public Guid Id { get; set; }

  [Required]
  public Guid RequestId { get; set; }

  [Required]
  public RequestStatusEnum Status { get; set; }

  public Guid? ChangedByUserId { get; set; }

  [MaxLength(1000)]
  public string? AdminNotes { get; set; }

  public DateTime Timestamp { get; set; }

  public PrintRequest Request { get; set; } = null!;

  public User? ChangedByUser { get; set; }
}
