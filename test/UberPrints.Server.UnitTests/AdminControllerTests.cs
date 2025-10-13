using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Controllers;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;

namespace UberPrints.Server.UnitTests;

public class AdminControllerTests : TestBase
{
  [Fact]
  public async Task GetAllRequests_ReturnsEmptyList_WhenNoRequestsExist()
  {
    // Arrange - No setup needed, using empty in-memory database

    // Act
    var result = await AdminController.GetAllRequests();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var requests = Assert.IsType<List<PrintRequestDto>>(okResult.Value);
    Assert.NotNull(requests);
    Assert.Empty(requests);
  }

  [Fact]
  public async Task GetAllRequests_ReturnsAllRequests_WhenRequestsExist()
  {
    // Arrange
    var testUser = TestDataFactory.CreateTestUser();
    var testFilament = TestDataFactory.CreateTestFilament();
    var testRequest = TestDataFactory.CreateTestPrintRequest(filament: testFilament);

    await Context.Users.AddAsync(testUser);
    await Context.Filaments.AddAsync(testFilament);
    await Context.PrintRequests.AddAsync(testRequest);
    await Context.SaveChangesAsync();

    // Act
    var result = await AdminController.GetAllRequests();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var requests = Assert.IsType<List<PrintRequestDto>>(okResult.Value);
    Assert.NotNull(requests);
    Assert.Single(requests);
    Assert.Equal(testRequest.Id, requests.First().Id);
  }

  [Fact]
  public async Task ChangeRequestStatus_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();
    var changeStatusDto = TestDataFactory.CreateChangeStatusDto();

    // Act
    var result = await AdminController.ChangeRequestStatus(nonExistentId, changeStatusDto);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task ChangeRequestStatus_UpdatesStatus_WhenRequestExists()
  {
    // Arrange
    var testUser = TestDataFactory.CreateTestUser();
    var testFilament = TestDataFactory.CreateTestFilament();
    var testRequest = TestDataFactory.CreateTestPrintRequest(filament: testFilament);

    await Context.Users.AddAsync(testUser);
    await Context.Filaments.AddAsync(testFilament);
    await Context.PrintRequests.AddAsync(testRequest);
    await Context.SaveChangesAsync();

    var changeStatusDto = TestDataFactory.CreateChangeStatusDto(RequestStatusEnum.Accepted, "Request accepted for processing");

    // Act
    var result = await AdminController.ChangeRequestStatus(testRequest.Id, changeStatusDto);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var responseDto = Assert.IsType<PrintRequestDto>(okResult.Value);

    // Verify the status was updated in the database
    var updatedRequest = await Context.PrintRequests
        .Include(pr => pr.StatusHistory)
        .FirstOrDefaultAsync(pr => pr.Id == testRequest.Id);

    Assert.NotNull(updatedRequest);
    Assert.Equal(RequestStatusEnum.Accepted, updatedRequest.CurrentStatus);
    Assert.Equal(2, updatedRequest.StatusHistory.Count); // Initial + new status

    // Find the latest status history entry (the one we just added)
    var latestStatusHistory = updatedRequest.StatusHistory
        .OrderByDescending(sh => sh.Timestamp)
        .First();

    Assert.Equal(RequestStatusEnum.Accepted, latestStatusHistory.Status);
    Assert.Equal("Request accepted for processing", latestStatusHistory.AdminNotes);
  }

  [Fact]
  public async Task CreateFilament_ReturnsCreatedFilament_WhenValidDataProvided()
  {
    // Arrange
    var createDto = TestDataFactory.CreateFilamentDto();

    // Act
    var result = await AdminController.CreateFilament(createDto);

    // Assert
    var createdResult = Assert.IsType<CreatedAtActionResult>(result);
    var filamentDto = Assert.IsType<FilamentDto>(createdResult.Value);
    Assert.NotNull(filamentDto);
    Assert.Equal(createDto.Name, filamentDto.Name);
    Assert.Equal(createDto.Material, filamentDto.Material);
    Assert.Equal(createDto.Brand, filamentDto.Brand);
    Assert.Equal(createDto.Colour, filamentDto.Colour);
    Assert.Equal(createDto.StockAmount, filamentDto.StockAmount);
    Assert.Equal(createDto.StockUnit, filamentDto.StockUnit);

    // Verify the filament was saved to the database
    var savedFilament = await Context.Filaments.FirstOrDefaultAsync(f => f.Id == filamentDto.Id);
    Assert.NotNull(savedFilament);
    Assert.Equal(createDto.Name, savedFilament.Name);
  }

  [Fact]
  public async Task CreateFilament_ThrowsNullReferenceException_WhenDtoIsNull()
  {
    // Arrange
    CreateFilamentDto? createDto = null;

    // Act & Assert
    await Assert.ThrowsAsync<NullReferenceException>(() => AdminController.CreateFilament(createDto!));
  }

  [Fact]
  public async Task CreateFilament_CreatesFilament_WhenNameIsEmpty()
  {
    // Arrange
    var createDto = TestDataFactory.CreateFilamentDto(name: "");

    // Act
    var result = await AdminController.CreateFilament(createDto);

    // Assert
    var createdResult = Assert.IsType<CreatedAtActionResult>(result);
    var filamentDto = Assert.IsType<FilamentDto>(createdResult.Value);
    Assert.NotNull(filamentDto);
    Assert.Equal("", filamentDto.Name); // Empty name is currently allowed
  }

  [Fact]
  public async Task CreateFilament_CreatesFilament_WhenStockAmountIsNegative()
  {
    // Arrange
    var createDto = TestDataFactory.CreateFilamentDto(stockAmount: -100);

    // Act
    var result = await AdminController.CreateFilament(createDto);

    // Assert
    var createdResult = Assert.IsType<CreatedAtActionResult>(result);
    var filamentDto = Assert.IsType<FilamentDto>(createdResult.Value);
    Assert.NotNull(filamentDto);
    Assert.Equal(-100, filamentDto.StockAmount); // Negative stock is currently allowed
  }

  [Fact]
  public async Task UpdateFilament_ReturnsNotFound_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();
    var updateDto = TestDataFactory.CreateUpdateFilamentDto();

    // Act
    var result = await AdminController.UpdateFilament(nonExistentId, updateDto);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task UpdateFilament_UpdatesFilament_WhenFilamentExists()
  {
    // Arrange
    var testFilament = TestDataFactory.CreateTestFilament();
    await Context.Filaments.AddAsync(testFilament);
    await Context.SaveChangesAsync();

    var updateDto = TestDataFactory.CreateUpdateFilamentDto(
        name: "Updated Filament Name",
        material: "ABS",
        brand: "Updated Brand",
        colour: "Red",
        stockAmount: 2000,
        stockUnit: "meters"
    );

    // Act
    var result = await AdminController.UpdateFilament(testFilament.Id, updateDto);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var responseDto = Assert.IsType<FilamentDto>(okResult.Value);

    // Verify the filament was updated in the database
    var updatedFilament = await Context.Filaments.FirstOrDefaultAsync(f => f.Id == testFilament.Id);
    Assert.NotNull(updatedFilament);
    Assert.Equal("Updated Filament Name", updatedFilament.Name);
    Assert.Equal("ABS", updatedFilament.Material);
    Assert.Equal("Updated Brand", updatedFilament.Brand);
    Assert.Equal("Red", updatedFilament.Colour);
    Assert.Equal(2000, updatedFilament.StockAmount);
    Assert.Equal("meters", updatedFilament.StockUnit);
  }

  [Fact]
  public async Task UpdateFilamentStock_ReturnsNotFound_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();
    var updateStockDto = TestDataFactory.CreateUpdateStockDto();

    // Act
    var result = await AdminController.UpdateFilamentStock(nonExistentId, updateStockDto);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task UpdateFilamentStock_UpdatesStock_WhenFilamentExists()
  {
    // Arrange
    var testFilament = TestDataFactory.CreateTestFilament(stockAmount: 1000);
    await Context.Filaments.AddAsync(testFilament);
    await Context.SaveChangesAsync();

    var updateStockDto = TestDataFactory.CreateUpdateStockDto(stockAmount: 1500);

    // Act
    var result = await AdminController.UpdateFilamentStock(testFilament.Id, updateStockDto);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var responseDto = Assert.IsType<FilamentDto>(okResult.Value);

    // Verify the stock was updated in the database
    var updatedFilament = await Context.Filaments.FirstOrDefaultAsync(f => f.Id == testFilament.Id);
    Assert.NotNull(updatedFilament);
    Assert.Equal(1500, updatedFilament.StockAmount);
  }

  [Fact]
  public async Task DeleteFilament_ReturnsNotFound_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var result = await AdminController.DeleteFilament(nonExistentId);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task DeleteFilament_DeletesFilament_WhenFilamentExists()
  {
    // Arrange
    var testFilament = TestDataFactory.CreateTestFilament();
    await Context.Filaments.AddAsync(testFilament);
    await Context.SaveChangesAsync();

    // Act
    var result = await AdminController.DeleteFilament(testFilament.Id);

    // Assert
    Assert.IsType<NoContentResult>(result);

    // Verify the filament was deleted from the database
    var deletedFilament = await Context.Filaments.FirstOrDefaultAsync(f => f.Id == testFilament.Id);
    Assert.Null(deletedFilament);
  }

  [Fact]
  public async Task GetFilament_ReturnsNotFound_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var result = await AdminController.GetFilament(nonExistentId);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task GetFilament_ReturnsFilament_WhenFilamentExists()
  {
    // Arrange
    var testFilament = TestDataFactory.CreateTestFilament();
    await Context.Filaments.AddAsync(testFilament);
    await Context.SaveChangesAsync();

    // Act
    var result = await AdminController.GetFilament(testFilament.Id);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var filamentDto = Assert.IsType<FilamentDto>(okResult.Value);
    Assert.NotNull(filamentDto);
    Assert.Equal(testFilament.Id, filamentDto.Id);
    Assert.Equal(testFilament.Name, filamentDto.Name);
    Assert.Equal(testFilament.Material, filamentDto.Material);
    Assert.Equal(testFilament.Brand, filamentDto.Brand);
    Assert.Equal(testFilament.Colour, filamentDto.Colour);
    Assert.Equal(testFilament.StockAmount, filamentDto.StockAmount);
    Assert.Equal(testFilament.StockUnit, filamentDto.StockUnit);
  }
}
