using UberPrints.Server.Models;

namespace UberPrints.Server.DTOs;

public class FilamentRequestStatusHistoryDto
{
  public Guid Id { get; set; }
  public FilamentRequestStatusEnum Status { get; set; }
  public string? Reason { get; set; }
  public Guid? ChangedByUserId { get; set; }
  public string? ChangedByUsername { get; set; }
  public DateTime CreatedAt { get; set; }
}
