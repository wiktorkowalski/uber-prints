namespace UberPrints.Server.DTOs;

public class PrintRequestChangeDto
{
  public Guid Id { get; set; }
  public Guid PrintRequestId { get; set; }
  public string FieldName { get; set; } = string.Empty;
  public string? OldValue { get; set; }
  public string? NewValue { get; set; }
  public Guid? ChangedByUserId { get; set; }
  public string? ChangedByUsername { get; set; }
  public DateTime ChangedAt { get; set; }
}
