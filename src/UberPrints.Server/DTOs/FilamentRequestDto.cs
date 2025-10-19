using UberPrints.Server.Models;

namespace UberPrints.Server.DTOs;

public class FilamentRequestDto
{
  public Guid Id { get; set; }
  public Guid? UserId { get; set; }
  public string RequesterName { get; set; } = string.Empty;
  public string Material { get; set; } = string.Empty;
  public string Brand { get; set; } = string.Empty;
  public string Colour { get; set; } = string.Empty;
  public string? Link { get; set; }
  public string? Notes { get; set; }
  public FilamentRequestStatusEnum CurrentStatus { get; set; }
  public Guid? FilamentId { get; set; }
  public string? FilamentName { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public List<FilamentRequestStatusHistoryDto> StatusHistory { get; set; } = new();
}
