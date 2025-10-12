namespace UberPrints.Server.DTOs;

public class PrintRequestDto
{
  public Guid Id { get; set; }
  public Guid? UserId { get; set; }
  public string? GuestTrackingToken { get; set; }
  public string RequesterName { get; set; } = string.Empty;
  public string ModelUrl { get; set; } = string.Empty;
  public string? Notes { get; set; }
  public bool RequestDelivery { get; set; }
  public Guid FilamentId { get; set; }
  public string FilamentName { get; set; } = string.Empty;
  public Models.RequestStatusEnum CurrentStatus { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public List<StatusHistoryDto> StatusHistory { get; set; } = new();
}
