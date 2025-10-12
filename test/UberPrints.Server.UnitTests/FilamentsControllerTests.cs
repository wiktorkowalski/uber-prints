using Microsoft.AspNetCore.Mvc;
using UberPrints.Server.Controllers;
using UberPrints.Server.DTOs;

namespace UberPrints.Server.UnitTests;

public class FilamentsControllerTests : TestBase
{
  [Fact]
  public async Task GetFilaments_ReturnsEmptyList_WhenNoFilamentsExist()
  {
    // Arrange - No setup needed, using empty in-memory database

    // Act
    var result = await FilamentsController.GetFilaments();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var filaments = Assert.IsType<List<FilamentDto>>(okResult.Value);
    Assert.NotNull(filaments);
    Assert.Empty(filaments);
  }

  [Fact]
  public async Task GetFilaments_ReturnsAllFilaments_WhenFilamentsExist()
  {
    // Arrange
    var testFilament1 = TestDataFactory.CreateTestFilament(name: "PLA White");
    var testFilament2 = TestDataFactory.CreateTestFilament(name: "ABS Black");

    await Context.Filaments.AddRangeAsync(testFilament1, testFilament2);
    await Context.SaveChangesAsync();

    // Act
    var result = await FilamentsController.GetFilaments();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var filaments = Assert.IsType<List<FilamentDto>>(okResult.Value);
    Assert.NotNull(filaments);
    Assert.Equal(2, filaments.Count);

    var filamentNames = filaments.Select(f => f.Name).ToList();
    Assert.Contains("PLA White", filamentNames);
    Assert.Contains("ABS Black", filamentNames);
  }

  [Fact]
  public async Task GetFilament_ReturnsNotFound_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var result = await FilamentsController.GetFilament(nonExistentId);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task GetFilament_ReturnsFilament_WhenFilamentExists()
  {
    // Arrange
    var testFilament = TestDataFactory.CreateTestFilament(
        name: "Premium PLA",
        material: "PLA",
        brand: "Hatchbox",
        colour: "Blue",
        stockAmount: 2500,
        stockUnit: "grams"
    );

    await Context.Filaments.AddAsync(testFilament);
    await Context.SaveChangesAsync();

    // Act
    var result = await FilamentsController.GetFilament(testFilament.Id);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var filamentDto = Assert.IsType<FilamentDto>(okResult.Value);
    Assert.NotNull(filamentDto);

    Assert.Equal(testFilament.Id, filamentDto.Id);
    Assert.Equal("Premium PLA", filamentDto.Name);
    Assert.Equal("PLA", filamentDto.Material);
    Assert.Equal("Hatchbox", filamentDto.Brand);
    Assert.Equal("Blue", filamentDto.Colour);
    Assert.Equal(2500, filamentDto.StockAmount);
    Assert.Equal("grams", filamentDto.StockUnit);
  }
}
