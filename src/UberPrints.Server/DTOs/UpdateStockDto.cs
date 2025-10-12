using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.DTOs;

public class UpdateStockDto
{
  [Required]
  [Range(0, double.MaxValue)]
  public decimal StockAmount { get; set; }
}
