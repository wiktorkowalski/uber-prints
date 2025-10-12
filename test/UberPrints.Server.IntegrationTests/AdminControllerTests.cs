using System.Net;
using System.Net.Http.Json;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class AdminControllerTests : IntegrationTestBase
{
  public AdminControllerTests(IntegrationTestFactory factory) : base(factory)
  {
  }

  #region Admin Requests Tests

  [Fact]
  public async Task GetAllRequests_ReturnsEmptyList_WhenNoRequestsExist()
  {
    // Act
    var response = await Client.GetAsync("/api/admin/requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<PrintRequestDto>>();
    Assert.NotNull(requests);
    Assert.Empty(requests);
  }

  [Fact]
  public async Task GetAllRequests_ReturnsAllRequests_WhenRequestsExist()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    await Client.PostAsJsonAsync("/api/requests", TestDataFactory.CreatePrintRequestDto(filament.Id, "User 1"));
    await Client.PostAsJsonAsync("/api/requests", TestDataFactory.CreatePrintRequestDto(filament.Id, "User 2"));
    await Client.PostAsJsonAsync("/api/requests", TestDataFactory.CreatePrintRequestDto(filament.Id, "User 3"));

    // Act
    var response = await Client.GetAsync("/api/admin/requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<PrintRequestDto>>();
    Assert.NotNull(requests);
    Assert.Equal(3, requests.Count);
  }

  [Fact]
  public async Task ChangeRequestStatus_UpdatesStatus_WhenValidDataProvided()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Test User");
    var requestResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var request = await requestResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);
    var changeStatusDto = new ChangeStatusDto
    {
      Status = RequestStatusEnum.Accepted,
      AdminNotes = "Request approved for processing"
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/requests/{request.Id}/status", changeStatusDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Equal(RequestStatusEnum.Accepted, updatedRequest.CurrentStatus);
    Assert.Equal(2, updatedRequest.StatusHistory.Count);

    var latestHistory = updatedRequest.StatusHistory.OrderByDescending(h => h.Timestamp).First();
    Assert.Equal(RequestStatusEnum.Accepted, latestHistory.Status);
    Assert.Equal("Request approved for processing", latestHistory.AdminNotes);
  }

  [Fact]
  public async Task ChangeRequestStatus_CreatesStatusHistory_WithMultipleChanges()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Test User");
    var requestResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var request = await requestResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);

    // Act - Make multiple status changes
    await Client.PutAsJsonAsync($"/api/admin/requests/{request.Id}/status", new ChangeStatusDto
    {
      Status = RequestStatusEnum.Accepted,
      AdminNotes = "Accepted"
    });

    await Client.PutAsJsonAsync($"/api/admin/requests/{request.Id}/status", new ChangeStatusDto
    {
      Status = RequestStatusEnum.OnHold,
      AdminNotes = "Waiting for materials"
    });

    var response = await Client.PutAsJsonAsync($"/api/admin/requests/{request.Id}/status", new ChangeStatusDto
    {
      Status = RequestStatusEnum.Completed,
      AdminNotes = "Work completed"
    });

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Equal(RequestStatusEnum.Completed, updatedRequest.CurrentStatus);
    Assert.Equal(4, updatedRequest.StatusHistory.Count); // Initial Pending + 3 changes
  }

  [Fact]
  public async Task ChangeRequestStatus_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();
    var changeStatusDto = new ChangeStatusDto
    {
      Status = RequestStatusEnum.Accepted,
      AdminNotes = "Notes"
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/requests/{nonExistentId}/status", changeStatusDto);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  #endregion

  #region Admin Filaments Tests

  [Fact]
  public async Task CreateFilament_ReturnsCreatedFilament_WhenValidDataProvided()
  {
    // Arrange
    var createDto = new CreateFilamentDto
    {
      Name = "Premium PLA",
      Material = "PLA+",
      Brand = "Premium Brand",
      Colour = "Blue",
      StockAmount = 2000,
      StockUnit = "grams",
      Link = "https://example.com/premium",
      PhotoUrl = "https://example.com/premium.jpg"
    };

    // Act
    var response = await Client.PostAsJsonAsync("/api/admin/filaments", createDto);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var createdFilament = await response.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(createdFilament);
    Assert.NotEqual(Guid.Empty, createdFilament.Id);
    Assert.Equal("Premium PLA", createdFilament.Name);
    Assert.Equal("PLA+", createdFilament.Material);
    Assert.Equal("Premium Brand", createdFilament.Brand);
    Assert.Equal("Blue", createdFilament.Colour);
    Assert.Equal(2000, createdFilament.StockAmount);
    Assert.Equal("grams", createdFilament.StockUnit);
    Assert.Equal("https://example.com/premium", createdFilament.Link);
    Assert.Equal("https://example.com/premium.jpg", createdFilament.PhotoUrl);
    Assert.NotEqual(default, createdFilament.CreatedAt);
    Assert.NotEqual(default, createdFilament.UpdatedAt);
  }

  [Fact]
  public async Task UpdateFilament_ReturnsUpdatedFilament_WhenValidDataProvided()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);
    var updateDto = new UpdateFilamentDto
    {
      Name = "Updated Name",
      Material = "ABS",
      Brand = "Updated Brand",
      Colour = "Black",
      StockAmount = 1500,
      StockUnit = "meters",
      Link = "https://example.com/updated",
      PhotoUrl = "https://example.com/updated.jpg"
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/filaments/{filament.Id}", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedFilament = await response.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(updatedFilament);
    Assert.Equal(filament.Id, updatedFilament.Id);
    Assert.Equal("Updated Name", updatedFilament.Name);
    Assert.Equal("ABS", updatedFilament.Material);
    Assert.Equal("Updated Brand", updatedFilament.Brand);
    Assert.Equal("Black", updatedFilament.Colour);
    Assert.Equal(1500, updatedFilament.StockAmount);
    Assert.Equal("meters", updatedFilament.StockUnit);
    Assert.Equal("https://example.com/updated", updatedFilament.Link);
    Assert.Equal("https://example.com/updated.jpg", updatedFilament.PhotoUrl);
    Assert.True(updatedFilament.UpdatedAt > updatedFilament.CreatedAt);
  }

  [Fact]
  public async Task UpdateFilament_ReturnsNotFound_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();
    var updateDto = new UpdateFilamentDto
    {
      Name = "Name",
      Material = "PLA",
      Brand = "Brand",
      Colour = "White",
      StockAmount = 100,
      StockUnit = "grams",
      Link = "https://example.com",
      PhotoUrl = "https://example.com/photo.jpg"
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/filaments/{nonExistentId}", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task UpdateFilamentStock_UpdatesStockAmount_WhenValidDataProvided()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto(stockAmount: 1000);
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);
    var updateStockDto = new UpdateStockDto
    {
      StockAmount = 500
    };

    // Act
    var response = await Client.PatchAsync($"/api/admin/filaments/{filament.Id}/stock",
        JsonContent.Create(updateStockDto));

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedFilament = await response.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(updatedFilament);
    Assert.Equal(filament.Id, updatedFilament.Id);
    Assert.Equal(500, updatedFilament.StockAmount);
    Assert.Equal(filament.Name, updatedFilament.Name);
    Assert.True(updatedFilament.UpdatedAt > filament.UpdatedAt);
  }

  [Fact]
  public async Task UpdateFilamentStock_ReturnsNotFound_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();
    var updateStockDto = new UpdateStockDto
    {
      StockAmount = 500
    };

    // Act
    var response = await Client.PatchAsync($"/api/admin/filaments/{nonExistentId}/stock",
        JsonContent.Create(updateStockDto));

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task DeleteFilament_ReturnsNoContent_WhenFilamentExistsAndHasNoActiveRequests()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    // Act
    var response = await Client.DeleteAsync($"/api/admin/filaments/{filament.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    // Verify deletion
    var getResponse = await Client.GetAsync($"/api/filaments/{filament.Id}");
    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteFilament_ReturnsNotFound_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await Client.DeleteAsync($"/api/admin/filaments/{nonExistentId}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task DeleteFilament_ReturnsBadRequest_WhenFilamentHasActiveRequests()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Test User");
    await Client.PostAsJsonAsync("/api/requests", requestDto);

    // Act
    var response = await Client.DeleteAsync($"/api/admin/filaments/{filament.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var errorMessage = await response.Content.ReadAsStringAsync();
    Assert.Contains("Cannot delete filament with active requests", errorMessage);
  }

  [Fact]
  public async Task GetFilament_ReturnsFilament_WhenFilamentExists()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var createdFilament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(createdFilament);

    // Act
    var response = await Client.GetAsync($"/api/admin/filaments/{createdFilament.Id}");

    // Assert
    response.EnsureSuccessStatusCode();
    var filament = await response.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);
    Assert.Equal(createdFilament.Id, filament.Id);
    Assert.Equal(createdFilament.Name, filament.Name);
  }

  [Fact]
  public async Task GetFilament_ReturnsNotFound_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await Client.GetAsync($"/api/admin/filaments/{nonExistentId}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  #endregion

}
