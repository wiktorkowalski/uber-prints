using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Controllers;
using UberPrints.Server.Models;
using UberPrints.Server.DTOs;

namespace UberPrints.Server.UnitTests;

public class RequestsControllerTests : TestBase
{
  [Fact]
  public async Task GetRequests_ReturnsEmptyList_WhenNoRequestsExist()
  {
    // Arrange - No setup needed, using empty in-memory database

    // Act
    var result = await RequestsController.GetRequests();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var requests = Assert.IsType<List<PrintRequestDto>>(okResult.Value);
    Assert.NotNull(requests);
    Assert.Empty(requests);
  }

  [Fact]
  public async Task GetRequests_ReturnsAllRequests_WhenRequestsExist()
  {
    // Arrange
    var testUser = TestDataFactory.CreateTestUser();
    var testFilament = TestDataFactory.CreateTestFilament();
    var testRequest1 = TestDataFactory.CreateTestPrintRequest(
        userId: testUser.Id,
        filament: testFilament,
        requesterName: "John Doe"
    );
    var testRequest2 = TestDataFactory.CreateTestPrintRequest(
        userId: testUser.Id,
        filament: testFilament,
        requesterName: "Jane Smith"
    );

    await Context.Users.AddAsync(testUser);
    await Context.Filaments.AddAsync(testFilament);
    await Context.PrintRequests.AddRangeAsync(testRequest1, testRequest2);
    await Context.SaveChangesAsync();

    // Act
    var result = await RequestsController.GetRequests();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var requests = Assert.IsType<List<PrintRequestDto>>(okResult.Value);
    Assert.NotNull(requests);
    Assert.Equal(2, requests.Count);

    var requesterNames = requests.Select(r => r.RequesterName).ToList();
    Assert.Contains("John Doe", requesterNames);
    Assert.Contains("Jane Smith", requesterNames);
  }

  [Fact]
  public async Task GetRequest_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var result = await RequestsController.GetRequest(nonExistentId);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task GetRequest_ReturnsRequest_WhenRequestExists()
  {
    // Arrange
    var testUser = TestDataFactory.CreateTestUser();
    var testFilament = TestDataFactory.CreateTestFilament();
    var testRequest = TestDataFactory.CreateTestPrintRequest(
        userId: testUser.Id,
        filament: testFilament,
        requesterName: "Test User",
        modelUrl: "https://example.com/model.stl",
        notes: "Test print request"
    );

    await Context.Users.AddAsync(testUser);
    await Context.Filaments.AddAsync(testFilament);
    await Context.PrintRequests.AddAsync(testRequest);
    await Context.SaveChangesAsync();

    // Act
    var result = await RequestsController.GetRequest(testRequest.Id);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var requestDto = Assert.IsType<PrintRequestDto>(okResult.Value);
    Assert.NotNull(requestDto);

    Assert.Equal(testRequest.Id, requestDto.Id);
    Assert.Equal("Test User", requestDto.RequesterName);
    Assert.Equal("https://example.com/model.stl", requestDto.ModelUrl);
    Assert.Equal("Test print request", requestDto.Notes);
    Assert.Equal(testFilament.Id, requestDto.FilamentId);
    Assert.Equal(testFilament.Name, requestDto.FilamentName);
  }

  [Fact]
  public async Task TrackRequest_ReturnsNotFound_WhenTokenDoesNotExist()
  {
    // Arrange
    var nonExistentToken = "NONEXISTENT123";

    // Act
    var result = await RequestsController.TrackRequest(nonExistentToken);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task TrackRequest_ReturnsRequest_WhenValidTokenProvided()
  {
    // Arrange
    var testUser = TestDataFactory.CreateTestUser();
    var testFilament = TestDataFactory.CreateTestFilament();
    var testRequest = TestDataFactory.CreateTestPrintRequest(
        userId: testUser.Id,
        filament: testFilament,
        guestTrackingToken: "VALIDTOKEN123"
    );

    await Context.Users.AddAsync(testUser);
    await Context.Filaments.AddAsync(testFilament);
    await Context.PrintRequests.AddAsync(testRequest);
    await Context.SaveChangesAsync();

    // Act
    var result = await RequestsController.TrackRequest("VALIDTOKEN123");

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var requestDto = Assert.IsType<PrintRequestDto>(okResult.Value);
    Assert.NotNull(requestDto);

    Assert.Equal(testRequest.Id, requestDto.Id);
    Assert.Equal("VALIDTOKEN123", requestDto.GuestTrackingToken);
  }

  [Fact]
  public async Task CreateRequest_ReturnsBadRequest_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentFilamentId = Guid.NewGuid();
    var createDto = TestDataFactory.CreatePrintRequestDto(nonExistentFilamentId);

    // Act
    var result = await RequestsController.CreateRequest(createDto);

    // Assert
    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    Assert.Equal("Invalid filament selected.", badRequestResult.Value);
  }

  [Fact]
  public async Task CreateRequest_CreatesRequest_WhenValidDataProvided()
  {
    // Arrange
    var testUser = TestDataFactory.CreateTestUser();
    var testFilament = TestDataFactory.CreateTestFilament(stockAmount: 1000);

    await Context.Users.AddAsync(testUser);
    await Context.Filaments.AddAsync(testFilament);
    await Context.SaveChangesAsync();

    var createDto = TestDataFactory.CreatePrintRequestDto(
        testFilament.Id,
        requesterName: "New User",
        modelUrl: "https://example.com/new-model.stl",
        notes: "New print request"
    );

    // Act
    var result = await RequestsController.CreateRequest(createDto);

    // Assert
    var createdResult = Assert.IsType<CreatedAtActionResult>(result);
    var requestDto = Assert.IsType<PrintRequestDto>(createdResult.Value);
    Assert.NotNull(requestDto);

    Assert.Equal("New User", requestDto.RequesterName);
    Assert.Equal("https://example.com/new-model.stl", requestDto.ModelUrl);
    Assert.Equal("New print request", requestDto.Notes);
    Assert.Equal(testFilament.Id, requestDto.FilamentId);
    Assert.Equal(testFilament.Name, requestDto.FilamentName);

    // Verify the request was saved to the database
    var savedRequest = await Context.PrintRequests
        .Include(pr => pr.Filament)
        .FirstOrDefaultAsync(pr => pr.Id == requestDto.Id);

    Assert.NotNull(savedRequest);
    Assert.Equal("New User", savedRequest.RequesterName);
    Assert.Equal(testFilament.Id, savedRequest.FilamentId);
  }

  [Fact]
  public async Task UpdateRequest_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();
    var updateDto = TestDataFactory.CreateUpdatePrintRequestDto();

    // Act
    var result = await RequestsController.UpdateRequest(nonExistentId, updateDto);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task UpdateRequest_ReturnsBadRequest_WhenFilamentOutOfStock()
  {
    // Arrange
    var testUser = TestDataFactory.CreateTestUser();
    var testFilament = TestDataFactory.CreateTestFilament(stockAmount: 0); // Out of stock
    var testRequest = TestDataFactory.CreateTestPrintRequest(
        userId: testUser.Id,
        filament: testFilament
    );

    await Context.Users.AddAsync(testUser);
    await Context.Filaments.AddAsync(testFilament);
    await Context.PrintRequests.AddAsync(testRequest);
    await Context.SaveChangesAsync();

    var updateDto = TestDataFactory.CreateUpdatePrintRequestDto(
        requesterName: "Updated User",
        filamentId: testFilament.Id
    );

    // Act
    var result = await RequestsController.UpdateRequest(testRequest.Id, updateDto);

    // Assert
    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    Assert.Equal("Selected filament is out of stock.", badRequestResult.Value);
  }

  [Fact]
  public async Task UpdateRequest_UpdatesRequest_WhenRequestExistsAndFilamentInStock()
  {
    // Arrange
    var testUser = TestDataFactory.CreateTestUser();
    var testFilament = TestDataFactory.CreateTestFilament(stockAmount: 1000); // In stock
    var testRequest = TestDataFactory.CreateTestPrintRequest(
        userId: testUser.Id,
        filament: testFilament
    );

    await Context.Users.AddAsync(testUser);
    await Context.Filaments.AddAsync(testFilament);
    await Context.PrintRequests.AddAsync(testRequest);
    await Context.SaveChangesAsync();

    var updateDto = TestDataFactory.CreateUpdatePrintRequestDto(
        requesterName: "Updated User",
        modelUrl: "https://example.com/updated-model.stl",
        notes: "Updated notes",
        requestDelivery: false,
        filamentId: testFilament.Id
    );

    // Act
    var result = await RequestsController.UpdateRequest(testRequest.Id, updateDto);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var responseDto = Assert.IsType<PrintRequestDto>(okResult.Value);

    // Verify the request was updated in the database
    var updatedRequest = await Context.PrintRequests.FirstOrDefaultAsync(pr => pr.Id == testRequest.Id);
    Assert.NotNull(updatedRequest);
    Assert.Equal("Updated User", updatedRequest.RequesterName);
    Assert.Equal("https://example.com/updated-model.stl", updatedRequest.ModelUrl);
    Assert.Equal("Updated notes", updatedRequest.Notes);
    Assert.False(updatedRequest.RequestDelivery);
  }

  [Fact]
  public async Task DeleteRequest_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var result = await RequestsController.DeleteRequest(nonExistentId);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task DeleteRequest_DeletesRequest_WhenRequestExists()
  {
    // Arrange
    var testUser = TestDataFactory.CreateTestUser();
    var testFilament = TestDataFactory.CreateTestFilament();
    var testRequest = TestDataFactory.CreateTestPrintRequest(
        userId: testUser.Id,
        filament: testFilament
    );

    await Context.Users.AddAsync(testUser);
    await Context.Filaments.AddAsync(testFilament);
    await Context.PrintRequests.AddAsync(testRequest);
    await Context.SaveChangesAsync();

    // Act
    var result = await RequestsController.DeleteRequest(testRequest.Id);

    // Assert
    Assert.IsType<NoContentResult>(result);

    // Verify the request was deleted from the database
    var deletedRequest = await Context.PrintRequests.FirstOrDefaultAsync(pr => pr.Id == testRequest.Id);
    Assert.Null(deletedRequest);
  }
}
