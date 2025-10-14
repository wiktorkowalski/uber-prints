using System.Net;
using System.Net.Http.Json;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class RequestsControllerTests : IntegrationTestBase
{
  public RequestsControllerTests(IntegrationTestFactory factory) : base(factory)
  {
  }

  [Fact]
  public async Task GetRequests_ReturnsEmptyList_WhenNoRequestsExist()
  {
    // Act
    var response = await Client.GetAsync("/api/requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<PrintRequestDto>>();
    Assert.NotNull(requests);
    Assert.Empty(requests);
  }

  [Fact]
  public async Task GetRequests_ReturnsAllRequests_WhenRequestsExist()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var request1Dto = TestDataFactory.CreatePrintRequestDto(filament.Id, "User 1");
    var request1Response = await Client.PostAsJsonAsync("/api/requests", request1Dto);
    request1Response.EnsureSuccessStatusCode();

    var request2Dto = TestDataFactory.CreatePrintRequestDto(filament.Id, "User 2");
    var request2Response = await Client.PostAsJsonAsync("/api/requests", request2Dto);
    request2Response.EnsureSuccessStatusCode();

    // Act
    var response = await Client.GetAsync("/api/requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<PrintRequestDto>>();
    Assert.NotNull(requests);
    Assert.Equal(2, requests.Count);
    Assert.Contains(requests, r => r.RequesterName == "User 1");
    Assert.Contains(requests, r => r.RequesterName == "User 2");
  }

  [Fact]
  public async Task GetRequestById_ReturnsRequest_WhenRequestExists()
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

    // Act
    var response = await Client.GetAsync($"/api/requests/{createdRequest.Id}");

    // Assert
    response.EnsureSuccessStatusCode();
    var request = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);
    Assert.Equal(createdRequest.Id, request.Id);
    Assert.Equal("Test User", request.RequesterName);
    Assert.Equal(filament.Id, request.FilamentId);
    Assert.Equal(filament.Name, request.FilamentName);
    Assert.NotNull(request.StatusHistory);
    Assert.Single(request.StatusHistory);
  }

  [Fact]
  public async Task GetRequestById_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await Client.GetAsync($"/api/requests/{nonExistentId}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task TrackRequest_ReturnsRequest_WhenValidTokenProvided()
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

    // Act
    var response = await Client.GetAsync($"/api/requests/track/{createdRequest.GuestTrackingToken}");

    // Assert
    response.EnsureSuccessStatusCode();
    var request = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);
    Assert.Equal(createdRequest.Id, request.Id);
    Assert.Equal(createdRequest.GuestTrackingToken, request.GuestTrackingToken);
  }

  [Fact]
  public async Task TrackRequest_ReturnsNotFound_WhenInvalidTokenProvided()
  {
    // Arrange
    var invalidToken = "INVALIDTOKEN123";

    // Act
    var response = await Client.GetAsync($"/api/requests/track/{invalidToken}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task CreateRequest_ReturnsCreatedRequest_WhenValidDataProvided()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    filamentResponse.EnsureSuccessStatusCode(); // Add explicit check
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);
    var createDto = new CreatePrintRequestDto
    {
      RequesterName = "John Doe",
      ModelUrl = "https://example.com/model.stl",
      Notes = "Test notes",
      RequestDelivery = true,
      FilamentId = filament.Id
    };

    // Act
    var response = await Client.PostAsJsonAsync("/api/requests", createDto);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var createdRequest = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(createdRequest);
    Assert.Equal("John Doe", createdRequest.RequesterName);
    Assert.Equal("https://example.com/model.stl", createdRequest.ModelUrl);
    Assert.Equal("Test notes", createdRequest.Notes);
    Assert.True(createdRequest.RequestDelivery);
    Assert.Equal(filament.Id, createdRequest.FilamentId);
    Assert.Equal(RequestStatusEnum.Pending, createdRequest.CurrentStatus);
    Assert.NotNull(createdRequest.GuestTrackingToken);
    Assert.NotEmpty(createdRequest.GuestTrackingToken);
    Assert.NotNull(createdRequest.StatusHistory);
    Assert.Single(createdRequest.StatusHistory);
    Assert.Equal(RequestStatusEnum.Pending, createdRequest.StatusHistory[0].Status);
  }

  [Fact]
  public async Task CreateRequest_ReturnsBadRequest_WhenFilamentDoesNotExist()
  {
    // Arrange
    var nonExistentFilamentId = Guid.NewGuid();
    var createDto = new CreatePrintRequestDto
    {
      RequesterName = "John Doe",
      ModelUrl = "https://example.com/model.stl",
      Notes = "Test notes",
      RequestDelivery = true,
      FilamentId = nonExistentFilamentId
    };

    // Act
    var response = await Client.PostAsJsonAsync("/api/requests", createDto);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var errorMessage = await response.Content.ReadAsStringAsync();
    Assert.Contains("Invalid filament selected", errorMessage);
  }

  [Fact]
  public async Task CreateRequest_ReturnsBadRequest_WhenFilamentIsOutOfStock()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto(stockAmount: 0);
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var outOfStockFilament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(outOfStockFilament);
    var createDto = new CreatePrintRequestDto
    {
      RequesterName = "John Doe",
      ModelUrl = "https://example.com/model.stl",
      Notes = "Test notes",
      RequestDelivery = true,
      FilamentId = outOfStockFilament.Id
    };

    // Act
    var response = await Client.PostAsJsonAsync("/api/requests", createDto);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var errorMessage = await response.Content.ReadAsStringAsync();
    Assert.Contains("out of stock", errorMessage);
  }

  [Fact]
  public async Task UpdateRequest_ReturnsUpdatedRequest_WhenValidDataProvided()
  {
    // Arrange
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

    var updateDto = new UpdatePrintRequestDto
    {
      RequesterName = "Updated Name",
      ModelUrl = "https://example.com/updated-model.stl",
      Notes = "Updated notes",
      RequestDelivery = false,
      FilamentId = filament2.Id
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/requests/{createdRequest.Id}", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Equal(createdRequest.Id, updatedRequest.Id);
    Assert.Equal("Updated Name", updatedRequest.RequesterName);
    Assert.Equal("https://example.com/updated-model.stl", updatedRequest.ModelUrl);
    Assert.Equal("Updated notes", updatedRequest.Notes);
    Assert.False(updatedRequest.RequestDelivery);
    Assert.Equal(filament2.Id, updatedRequest.FilamentId);
  }

  [Fact]
  public async Task UpdateRequest_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);
    var nonExistentId = Guid.NewGuid();
    var updateDto = new UpdatePrintRequestDto
    {
      RequesterName = "Updated Name",
      ModelUrl = "https://example.com/model.stl",
      Notes = "Notes",
      RequestDelivery = true,
      FilamentId = filament.Id
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/requests/{nonExistentId}", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task UpdateRequest_ReturnsBadRequest_WhenFilamentIsOutOfStock()
  {
    // Arrange
    var filament1Dto = TestDataFactory.CreateFilamentDto(stockAmount: 100);
    var filament1Response = await Client.PostAsJsonAsync("/api/admin/filaments", filament1Dto);
    var filament1 = await filament1Response.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament1);

    var filament2Dto = TestDataFactory.CreateFilamentDto(stockAmount: 0);
    var filament2Response = await Client.PostAsJsonAsync("/api/admin/filaments", filament2Dto);
    var filament2 = await filament2Response.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament2);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament1.Id, "Test User");
    var requestResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var createdRequest = await requestResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(createdRequest);

    var updateDto = new UpdatePrintRequestDto
    {
      RequesterName = "Test User",
      ModelUrl = "https://example.com/model.stl",
      Notes = "Notes",
      RequestDelivery = true,
      FilamentId = filament2.Id
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/requests/{createdRequest.Id}", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var errorMessage = await response.Content.ReadAsStringAsync();
    Assert.Contains("out of stock", errorMessage);
  }

  [Fact]
  public async Task DeleteRequest_ReturnsNoContent_WhenRequestExists()
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

    // Act
    var response = await Client.DeleteAsync($"/api/requests/{createdRequest.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    // Verify deletion
    var getResponse = await Client.GetAsync($"/api/requests/{createdRequest.Id}");
    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteRequest_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await Client.DeleteAsync($"/api/requests/{nonExistentId}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

}
