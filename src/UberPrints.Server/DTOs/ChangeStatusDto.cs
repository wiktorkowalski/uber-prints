using System.ComponentModel.DataAnnotations;
using UberPrints.Server.Models;

namespace UberPrints.Server.DTOs;

public class ChangeStatusDto
{
  [Required]
  public RequestStatusEnum Status { get; set; }

  [MaxLength(1000)]
  public string? AdminNotes { get; set; }
}
