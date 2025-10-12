namespace UberPrints.Server.DTOs;

public class StatusHistoryDto
{
  public Guid Id { get; set; }
  public Guid RequestId { get; set; }
  public Models.RequestStatusEnum Status { get; set; }
  public Guid? ChangedByUserId { get; set; }
  public string? ChangedByUsername { get; set; }
  public string? AdminNotes { get; set; }
  public DateTime Timestamp { get; set; }
}
