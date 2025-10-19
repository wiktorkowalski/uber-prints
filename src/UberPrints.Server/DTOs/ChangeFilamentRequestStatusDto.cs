using System.ComponentModel.DataAnnotations;
using UberPrints.Server.Models;

namespace UberPrints.Server.DTOs;

public class ChangeFilamentRequestStatusDto
{
  [Required]
  public FilamentRequestStatusEnum Status { get; set; }

  [MaxLength(500)]
  public string? Reason { get; set; }

  public Guid? FilamentId { get; set; }
}
