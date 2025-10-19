namespace UberPrints.Server.DTOs;

public class FilamentDto
{
  public Guid Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Material { get; set; } = string.Empty;
  public string Brand { get; set; } = string.Empty;
  public string Colour { get; set; } = string.Empty;
  public decimal StockAmount { get; set; }
  public string StockUnit { get; set; } = "grams";
  public string? Link { get; set; }
  public string? PhotoUrl { get; set; }
  public bool IsAvailable { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
