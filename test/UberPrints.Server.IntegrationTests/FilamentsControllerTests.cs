using System.Net;
using System.Net.Http.Json;
using UberPrints.Server.DTOs;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class FilamentsControllerTests : IntegrationTestBase
{
  public FilamentsControllerTests(IntegrationTestFactory factory) : base(factory)
  {
  }

  [Fact]
  public async Task GetFilaments_ReturnsEmptyList_WhenNoFilamentsExist()
  {
    // Act
    var response = await Client.GetAsync("/api/filaments");

    // Assert
    response.EnsureSuccessStatusCode();
    var filaments = await response.Content.ReadFromJsonAsync<List<FilamentDto>>();
    Assert.NotNull(filaments);
    Assert.Empty(filaments);
  }

  [Fact]
  public async Task GetFilaments_ReturnsAllFilaments_WhenFilamentsExist()
  {
    // Arrange
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("PLA White", stockAmount: 1000));
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("ABS Black", stockAmount: 500));
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("PETG Clear", stockAmount: 0));

    // Act
    var response = await Client.GetAsync("/api/filaments");

    // Assert
    response.EnsureSuccessStatusCode();
    var filaments = await response.Content.ReadFromJsonAsync<List<FilamentDto>>();
    Assert.NotNull(filaments);
    Assert.Equal(3, filaments.Count);
    Assert.Contains(filaments, f => f.Name == "PLA White");
    Assert.Contains(filaments, f => f.Name == "ABS Black");
    Assert.Contains(filaments, f => f.Name == "PETG Clear");
  }

  [Fact]
  public async Task GetFilaments_ReturnsFilamentsSortedByName()
  {
    // Arrange
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("Zebra Filament", stockAmount: 100));
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("Alpha Filament", stockAmount: 100));
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("Beta Filament", stockAmount: 100));

    // Act
    var response = await Client.GetAsync("/api/filaments");

    // Assert
    response.EnsureSuccessStatusCode();
    var filaments = await response.Content.ReadFromJsonAsync<List<FilamentDto>>();
    Assert.NotNull(filaments);
    Assert.Equal(3, filaments.Count);
    Assert.Equal("Alpha Filament", filaments[0].Name);
    Assert.Equal("Beta Filament", filaments[1].Name);
    Assert.Equal("Zebra Filament", filaments[2].Name);
  }

  [Fact]
  public async Task GetFilaments_WithInStockFilter_ReturnsOnlyInStockFilaments()
  {
    // Arrange
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("In Stock 1", stockAmount: 1000));
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("In Stock 2", stockAmount: 500));
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("Out of Stock 1", stockAmount: 0));
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("Out of Stock 2", stockAmount: 0));

    // Act
    var response = await Client.GetAsync("/api/filaments?inStock=true");

    // Assert
    response.EnsureSuccessStatusCode();
    var filaments = await response.Content.ReadFromJsonAsync<List<FilamentDto>>();
    Assert.NotNull(filaments);
    Assert.Equal(2, filaments.Count);
    Assert.All(filaments, f => Assert.True(f.StockAmount > 0));
    Assert.Contains(filaments, f => f.Name == "In Stock 1");
    Assert.Contains(filaments, f => f.Name == "In Stock 2");
  }

  [Fact]
  public async Task GetFilaments_WithInStockFilterFalse_ReturnsAllFilaments()
  {
    // Arrange
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("In Stock", stockAmount: 1000));
    await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("Out of Stock", stockAmount: 0));

    // Act
    var response = await Client.GetAsync("/api/filaments?inStock=false");

    // Assert
    response.EnsureSuccessStatusCode();
    var filaments = await response.Content.ReadFromJsonAsync<List<FilamentDto>>();
    Assert.NotNull(filaments);
    Assert.Equal(2, filaments.Count);
  }

  [Fact]
  public async Task GetFilamentById_ReturnsFilament_WhenFilamentExists()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto("Test Filament", stockAmount: 750);
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var createdFilament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(createdFilament);

    // Act
    var response = await Client.GetAsync($"/api/filaments/{createdFilament.Id}");

    // Assert
    response.EnsureSuccessStatusCode();
    var filament = await response.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);
    Assert.Equal(createdFilament.Id, filament.Id);
    Assert.Equal("Test Filament", filament.Name);
    Assert.Equal("PLA", filament.Material);
    Assert.Equal("Test Brand", filament.Brand);
    Assert.Equal("White", filament.Colour);
    Assert.Equal(750, filament.StockAmount);
    Assert.Equal("grams", filament.StockUnit);
    Assert.Equal("https://example.com/filament", filament.Link);
    Assert.Equal("https://example.com/photo.jpg", filament.PhotoUrl);
    Assert.NotEqual(default, filament.CreatedAt);
    Assert.NotEqual(default, filament.UpdatedAt);
  }

  [Fact]
  public async Task GetFilamentById_ReturnsNotFound_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await Client.GetAsync($"/api/filaments/{nonExistentId}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetFilaments_ReturnsFilamentsWithAllProperties()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto(
        name: "Premium PLA",
        material: "PLA+",
        brand: "Premium Brand",
        colour: "Metallic Blue",
        stockAmount: 1500,
        stockUnit: "grams",
        link: "https://example.com/premium-pla",
        photoUrl: "https://example.com/premium.jpg");
    await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);

    // Act
    var getResponse = await Client.GetAsync("/api/filaments");

    // Assert
    getResponse.EnsureSuccessStatusCode();
    var filaments = await getResponse.Content.ReadFromJsonAsync<List<FilamentDto>>();
    Assert.NotNull(filaments);
    var filament = Assert.Single(filaments);
    Assert.Equal("Premium PLA", filament.Name);
    Assert.Equal("PLA+", filament.Material);
    Assert.Equal("Premium Brand", filament.Brand);
    Assert.Equal("Metallic Blue", filament.Colour);
    Assert.Equal(1500, filament.StockAmount);
    Assert.Equal("grams", filament.StockUnit);
    Assert.Equal("https://example.com/premium-pla", filament.Link);
    Assert.Equal("https://example.com/premium.jpg", filament.PhotoUrl);
  }

}
