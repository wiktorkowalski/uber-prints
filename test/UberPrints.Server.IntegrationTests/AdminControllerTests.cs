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

  #region Admin Update Print Request Tests

  [Fact]
  public async Task AdminUpdateRequest_UpdatesAllFields_WhenValidDataProvided()
  {
    // Arrange - Create initial request
    var filament1Dto = TestDataFactory.CreateFilamentDto(name: "Filament 1");
    var filament1Response = await Client.PostAsJsonAsync("/api/admin/filaments", filament1Dto);
    var filament1 = await filament1Response.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament1);

    var filament2Dto = TestDataFactory.CreateFilamentDto(name: "Filament 2");
    var filament2Response = await Client.PostAsJsonAsync("/api/admin/filaments", filament2Dto);
    var filament2 = await filament2Response.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament2);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament1.Id, "Original Name");
    var requestResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var createdRequest = await requestResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(createdRequest);

    // Admin updates the request
    var updateDto = new UpdatePrintRequestAdminDto
    {
      RequesterName = "Admin Updated Name",
      ModelUrl = "https://example.com/admin-updated-model.stl",
      Notes = "Admin updated notes",
      RequestDelivery = false,
      IsPublic = false,
      FilamentId = filament2.Id
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/requests/{createdRequest.Id}", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Equal(createdRequest.Id, updatedRequest.Id);
    Assert.Equal("Admin Updated Name", updatedRequest.RequesterName);
    Assert.Equal("https://example.com/admin-updated-model.stl", updatedRequest.ModelUrl);
    Assert.Equal("Admin updated notes", updatedRequest.Notes);
    Assert.False(updatedRequest.RequestDelivery);
    Assert.False(updatedRequest.IsPublic);
    Assert.Equal(filament2.Id, updatedRequest.FilamentId);
  }

  [Fact]
  public async Task AdminUpdateRequest_CanSetFilamentIdToNull()
  {
    // Arrange - Create request with filament
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Test User");
    var requestResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var createdRequest = await requestResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(createdRequest);
    Assert.NotNull(createdRequest.FilamentId);

    // Admin removes filament
    var updateDto = new UpdatePrintRequestAdminDto
    {
      RequesterName = createdRequest.RequesterName,
      ModelUrl = createdRequest.ModelUrl,
      Notes = createdRequest.Notes,
      RequestDelivery = createdRequest.RequestDelivery,
      IsPublic = createdRequest.IsPublic,
      FilamentId = null // Remove filament
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/requests/{createdRequest.Id}", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Null(updatedRequest.FilamentId);
    Assert.Null(updatedRequest.FilamentName);
  }

  [Fact]
  public async Task AdminUpdateRequest_UpdatesIsPublicFlag()
  {
    // Arrange - Create public request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = new CreatePrintRequestDto
    {
      RequesterName = "Test User",
      ModelUrl = "https://example.com/model.stl",
      Notes = "Test request",
      RequestDelivery = true,
      IsPublic = true,
      FilamentId = filament.Id
    };
    var requestResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var createdRequest = await requestResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(createdRequest);
    Assert.True(createdRequest.IsPublic);

    // Admin makes it private
    var updateDto = new UpdatePrintRequestAdminDto
    {
      RequesterName = createdRequest.RequesterName,
      ModelUrl = createdRequest.ModelUrl,
      Notes = createdRequest.Notes,
      RequestDelivery = createdRequest.RequestDelivery,
      IsPublic = false, // Change to private
      FilamentId = createdRequest.FilamentId
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/requests/{createdRequest.Id}", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.False(updatedRequest.IsPublic);
  }

  [Fact]
  public async Task AdminUpdateRequest_UpdatesRequestDeliveryFlag()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Test User");
    var requestResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var createdRequest = await requestResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(createdRequest);

    var originalDeliveryValue = createdRequest.RequestDelivery;

    // Admin toggles delivery flag
    var updateDto = new UpdatePrintRequestAdminDto
    {
      RequesterName = createdRequest.RequesterName,
      ModelUrl = createdRequest.ModelUrl,
      Notes = createdRequest.Notes,
      RequestDelivery = !originalDeliveryValue, // Toggle
      IsPublic = createdRequest.IsPublic,
      FilamentId = createdRequest.FilamentId
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/requests/{createdRequest.Id}", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Equal(!originalDeliveryValue, updatedRequest.RequestDelivery);
  }

  [Fact]
  public async Task AdminUpdateRequest_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();
    var updateDto = new UpdatePrintRequestAdminDto
    {
      RequesterName = "Name",
      ModelUrl = "https://example.com/model.stl",
      Notes = "Notes",
      RequestDelivery = true,
      IsPublic = true,
      FilamentId = null
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/requests/{nonExistentId}", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task AdminUpdateRequest_ReturnsBadRequest_WhenFilamentIdIsInvalid()
  {
    // Arrange - Create request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Test User");
    var requestResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var createdRequest = await requestResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(createdRequest);

    // Admin tries to set invalid filament
    var nonExistentFilamentId = Guid.NewGuid();
    var updateDto = new UpdatePrintRequestAdminDto
    {
      RequesterName = createdRequest.RequesterName,
      ModelUrl = createdRequest.ModelUrl,
      Notes = createdRequest.Notes,
      RequestDelivery = createdRequest.RequestDelivery,
      IsPublic = createdRequest.IsPublic,
      FilamentId = nonExistentFilamentId // Invalid filament ID
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/requests/{createdRequest.Id}", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var errorMessage = await response.Content.ReadAsStringAsync();
    Assert.Contains("Invalid filament selected", errorMessage);
  }

  #endregion

  #region Admin Filament Requests Tests

  [Fact]
  public async Task GetAllFilamentRequests_ReturnsEmptyList_WhenNoRequestsExist()
  {
    // Act
    var response = await Client.GetAsync("/api/admin/filament-requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<FilamentRequestDto>>();
    Assert.NotNull(requests);
    Assert.Empty(requests);
  }

  [Fact]
  public async Task GetAllFilamentRequests_ReturnsAllRequests_WhenRequestsExist()
  {
    // Arrange - Create filament requests
    await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "User A",
      Material = "PLA",
      Brand = "Brand A",
      Colour = "Red"
    });

    await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "User B",
      Material = "PETG",
      Brand = "Brand B",
      Colour = "Blue"
    });

    await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "User C",
      Material = "ABS",
      Brand = "Brand C",
      Colour = "Green"
    });

    // Act
    var response = await Client.GetAsync("/api/admin/filament-requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<FilamentRequestDto>>();
    Assert.NotNull(requests);
    Assert.Equal(3, requests.Count);
    Assert.Contains(requests, r => r.RequesterName == "User A");
    Assert.Contains(requests, r => r.RequesterName == "User B");
    Assert.Contains(requests, r => r.RequesterName == "User C");
  }

  [Fact]
  public async Task GetAllFilamentRequests_ReturnsRequestsOrderedByCreatedAtDescending()
  {
    // Arrange - Create requests with slight delays
    var request1Response = await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "First",
      Material = "PLA",
      Brand = "Brand",
      Colour = "Red"
    });
    var request1 = await request1Response.Content.ReadFromJsonAsync<FilamentRequestDto>();

    await Task.Delay(10); // Small delay to ensure different timestamps

    var request2Response = await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Second",
      Material = "PETG",
      Brand = "Brand",
      Colour = "Blue"
    });
    var request2 = await request2Response.Content.ReadFromJsonAsync<FilamentRequestDto>();

    await Task.Delay(10);

    var request3Response = await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Third",
      Material = "ABS",
      Brand = "Brand",
      Colour = "Green"
    });
    var request3 = await request3Response.Content.ReadFromJsonAsync<FilamentRequestDto>();

    // Act
    var response = await Client.GetAsync("/api/admin/filament-requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<FilamentRequestDto>>();
    Assert.NotNull(requests);
    Assert.Equal(3, requests.Count);
    // Should be ordered newest first
    Assert.Equal("Third", requests[0].RequesterName);
    Assert.Equal("Second", requests[1].RequesterName);
    Assert.Equal("First", requests[2].RequesterName);
  }

  [Fact]
  public async Task GetAllFilamentRequests_IncludesStatusHistory()
  {
    // Arrange - Create and update a filament request
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "PLA",
      Brand = "Test Brand",
      Colour = "White"
    });
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    // Change status
    await Client.PutAsJsonAsync($"/api/admin/filament-requests/{createdRequest.Id}/status", new ChangeFilamentRequestStatusDto
    {
      Status = FilamentRequestStatusEnum.Approved,
      Reason = "Approved for purchase"
    });

    // Act
    var response = await Client.GetAsync("/api/admin/filament-requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<FilamentRequestDto>>();
    Assert.NotNull(requests);
    var request = Assert.Single(requests);
    Assert.NotNull(request.StatusHistory);
    Assert.Equal(2, request.StatusHistory.Count); // Initial Pending + Approved
  }

  [Fact]
  public async Task ChangeFilamentRequestStatus_UpdatesStatusToApproved()
  {
    // Arrange
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "PLA",
      Brand = "Prusament",
      Colour = "Galaxy Black"
    });
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);
    Assert.Equal(FilamentRequestStatusEnum.Pending, createdRequest.CurrentStatus);

    var statusDto = new ChangeFilamentRequestStatusDto
    {
      Status = FilamentRequestStatusEnum.Approved,
      Reason = "Great choice, we'll order this"
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/filament-requests/{createdRequest.Id}/status", statusDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Equal(FilamentRequestStatusEnum.Approved, updatedRequest.CurrentStatus);
    Assert.Equal(2, updatedRequest.StatusHistory.Count);

    var latestHistory = updatedRequest.StatusHistory.OrderByDescending(h => h.CreatedAt).First();
    Assert.Equal(FilamentRequestStatusEnum.Approved, latestHistory.Status);
    Assert.Equal("Great choice, we'll order this", latestHistory.Reason);
  }

  [Fact]
  public async Task ChangeFilamentRequestStatus_UpdatesStatusToRejected()
  {
    // Arrange
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "Expensive Material",
      Brand = "Premium Brand",
      Colour = "Gold"
    });
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    var statusDto = new ChangeFilamentRequestStatusDto
    {
      Status = FilamentRequestStatusEnum.Rejected,
      Reason = "Too expensive for our budget"
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/filament-requests/{createdRequest.Id}/status", statusDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Equal(FilamentRequestStatusEnum.Rejected, updatedRequest.CurrentStatus);

    var latestHistory = updatedRequest.StatusHistory.OrderByDescending(h => h.CreatedAt).First();
    Assert.Equal("Too expensive for our budget", latestHistory.Reason);
  }

  [Fact]
  public async Task ChangeFilamentRequestStatus_LinksFilamentWhenApproved()
  {
    // Arrange - Create a filament
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("Test Filament", stockAmount: 1000));
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    // Create filament request
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "PLA",
      Brand = "Test Brand",
      Colour = "Blue"
    });
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);
    Assert.Null(createdRequest.FilamentId);

    // Approve and link filament
    var statusDto = new ChangeFilamentRequestStatusDto
    {
      Status = FilamentRequestStatusEnum.Approved,
      FilamentId = filament.Id,
      Reason = "Linked to existing filament"
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/filament-requests/{createdRequest.Id}/status", statusDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Equal(FilamentRequestStatusEnum.Approved, updatedRequest.CurrentStatus);
    Assert.Equal(filament.Id, updatedRequest.FilamentId);
    Assert.Equal(filament.Name, updatedRequest.FilamentName);
  }

  [Fact]
  public async Task ChangeFilamentRequestStatus_TracksMultipleStatusChanges()
  {
    // Arrange
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "PLA",
      Brand = "Brand",
      Colour = "Red"
    });
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    // Act - Multiple status changes
    await Client.PutAsJsonAsync($"/api/admin/filament-requests/{createdRequest.Id}/status", new ChangeFilamentRequestStatusDto
    {
      Status = FilamentRequestStatusEnum.Pending,
      Reason = "Needs more info"
    });

    await Client.PutAsJsonAsync($"/api/admin/filament-requests/{createdRequest.Id}/status", new ChangeFilamentRequestStatusDto
    {
      Status = FilamentRequestStatusEnum.Approved,
      Reason = "Info received, approved"
    });

    var response = await Client.PutAsJsonAsync($"/api/admin/filament-requests/{createdRequest.Id}/status", new ChangeFilamentRequestStatusDto
    {
      Status = FilamentRequestStatusEnum.Received,
      Reason = "Filament purchased and added to inventory"
    });

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Equal(FilamentRequestStatusEnum.Received, updatedRequest.CurrentStatus);
    Assert.Equal(4, updatedRequest.StatusHistory.Count); // Initial + 3 changes

    var statuses = updatedRequest.StatusHistory.OrderBy(h => h.CreatedAt).Select(h => h.Status).ToList();
    Assert.Equal(FilamentRequestStatusEnum.Pending, statuses[0]);
    Assert.Equal(FilamentRequestStatusEnum.Pending, statuses[1]);
    Assert.Equal(FilamentRequestStatusEnum.Approved, statuses[2]);
    Assert.Equal(FilamentRequestStatusEnum.Received, statuses[3]);
  }

  [Fact]
  public async Task ChangeFilamentRequestStatus_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();
    var statusDto = new ChangeFilamentRequestStatusDto
    {
      Status = FilamentRequestStatusEnum.Approved,
      Reason = "Test"
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/filament-requests/{nonExistentId}/status", statusDto);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  #endregion

}
