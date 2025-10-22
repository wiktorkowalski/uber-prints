using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Models;

public class PrintRequestChange
{
  [Key]
  public Guid Id { get; set; }

  [Required]
  public Guid PrintRequestId { get; set; }

  [Required]
  [MaxLength(100)]
  public string FieldName { get; set; } = string.Empty;

  [MaxLength(2000)]
  public string? OldValue { get; set; }

  [MaxLength(2000)]
  public string? NewValue { get; set; }

  public Guid? ChangedByUserId { get; set; }

  [Required]
  public DateTime ChangedAt { get; set; }

  public PrintRequest PrintRequest { get; set; } = null!;

  public User? ChangedByUser { get; set; }
}
