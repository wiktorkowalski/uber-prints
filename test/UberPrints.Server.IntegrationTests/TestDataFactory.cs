using UberPrints.Server.DTOs;

namespace UberPrints.Server.IntegrationTests;

public static class TestDataFactory
{
  public static CreateFilamentDto CreateFilamentDto(
      string name = "Test Filament",
      string material = "PLA",
      string brand = "Test Brand",
      string colour = "White",
      int stockAmount = 1000,
      string stockUnit = "grams",
      string? link = null,
      string? photoUrl = null)
  {
    return new CreateFilamentDto
    {
      Name = name,
      Material = material,
      Brand = brand,
      Colour = colour,
      StockAmount = stockAmount,
      StockUnit = stockUnit,
      Link = link ?? "https://example.com/filament",
      PhotoUrl = photoUrl ?? "https://example.com/photo.jpg"
    };
  }

  public static CreatePrintRequestDto CreatePrintRequestDto(
      Guid filamentId,
      string requesterName = "Test User",
      string? modelUrl = null,
      string? notes = null,
      bool requestDelivery = true)
  {
    return new CreatePrintRequestDto
    {
      RequesterName = requesterName,
      ModelUrl = modelUrl ?? "https://example.com/model.stl",
      Notes = notes ?? "Test request",
      RequestDelivery = requestDelivery,
      FilamentId = filamentId
    };
  }
}
