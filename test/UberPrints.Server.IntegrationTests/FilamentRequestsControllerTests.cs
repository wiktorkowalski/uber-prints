using System.Net;
using System.Net.Http.Json;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class FilamentRequestsControllerTests : IntegrationTestBase
{
  public FilamentRequestsControllerTests(IntegrationTestFactory factory) : base(factory)
  {
  }

  [Fact]
  public async Task GetFilamentRequests_ReturnsEmptyList_WhenNoRequestsExist()
  {
    // Act
    var response = await Client.GetAsync("/api/filamentrequests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<FilamentRequestDto>>();
    Assert.NotNull(requests);
    Assert.Empty(requests);
  }

  [Fact]
  public async Task CreateFilamentRequest_ReturnsCreated_WhenValidDataProvided()
  {
    // Arrange
    var createDto = new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "PLA",
      Brand = "Prusament",
      Colour = "Galaxy Black",
      Link = "https://example.com/filament",
      Notes = "Please add this filament"
    };

    // Act
    var response = await Client.PostAsJsonAsync("/api/filamentrequests", createDto);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var request = await response.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(request);
    Assert.Equal("Test User", request.RequesterName);
    Assert.Equal("PLA", request.Material);
    Assert.Equal("Prusament", request.Brand);
    Assert.Equal("Galaxy Black", request.Colour);
    Assert.Equal(FilamentRequestStatusEnum.Pending, request.CurrentStatus);
    Assert.Single(request.StatusHistory); // Initial status
  }

  [Fact]
  public async Task GetFilamentRequest_ReturnsRequest_WhenRequestExists()
  {
    // Arrange
    var createDto = new CreateFilamentRequestDto
    {
      RequesterName = "John Doe",
      Material = "PETG",
      Brand = "Hatchbox",
      Colour = "Orange"
    };
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", createDto);
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    // Act
    var response = await Client.GetAsync($"/api/filamentrequests/{createdRequest.Id}");

    // Assert
    response.EnsureSuccessStatusCode();
    var request = await response.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(request);
    Assert.Equal(createdRequest.Id, request.Id);
    Assert.Equal("John Doe", request.RequesterName);
  }

  [Fact]
  public async Task GetFilamentRequest_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Act
    var response = await Client.GetAsync($"/api/filamentrequests/{Guid.NewGuid()}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task DeleteFilamentRequest_ReturnsNoContent_WhenRequestExists()
  {
    // Arrange
    var createDto = new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "ABS",
      Brand = "Generic",
      Colour = "White"
    };
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", createDto);
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    // Act
    var response = await Client.DeleteAsync($"/api/filamentrequests/{createdRequest.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    // Verify deletion
    var getResponse = await Client.GetAsync($"/api/filamentrequests/{createdRequest.Id}");
    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
  }

  [Fact]
  public async Task GetMyFilamentRequests_ReturnsOnlyUserRequests()
  {
    // Arrange - Create requests (they'll be associated with the test guest session)
    await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "User 1",
      Material = "PLA",
      Brand = "Brand A",
      Colour = "Red"
    });
    await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "User 1",
      Material = "PETG",
      Brand = "Brand B",
      Colour = "Blue"
    });

    // Act
    var response = await Client.GetAsync("/api/filamentrequests/my-requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<FilamentRequestDto>>();
    Assert.NotNull(requests);
    Assert.Equal(2, requests.Count);
  }

  [Fact]
  public async Task AdminGetFilamentRequests_ReturnsAllRequests()
  {
    // Arrange
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

    // Act
    var response = await Client.GetAsync("/api/admin/filament-requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<FilamentRequestDto>>();
    Assert.NotNull(requests);
    Assert.Equal(2, requests.Count);
  }

  [Fact]
  public async Task AdminChangeFilamentRequestStatus_UpdatesStatus_WhenValidDataProvided()
  {
    // Arrange
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "PLA",
      Brand = "Prusament",
      Colour = "Silver"
    });
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    var statusDto = new ChangeFilamentRequestStatusDto
    {
      Status = FilamentRequestStatusEnum.Approved,
      Reason = "Approved for purchase"
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/admin/filament-requests/{createdRequest.Id}/status", statusDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Equal(FilamentRequestStatusEnum.Approved, updatedRequest.CurrentStatus);
    Assert.Equal(2, updatedRequest.StatusHistory.Count); // Initial + Approved
    Assert.Contains(updatedRequest.StatusHistory, h => h.Reason == "Approved for purchase");
  }

  [Fact]
  public async Task AdminChangeFilamentRequestStatus_LinksFilament_WhenFilamentIdProvided()
  {
    // Arrange
    // Create a filament first
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", TestDataFactory.CreateFilamentDto("Test Filament", stockAmount: 1000));
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    // Create a filament request
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "PLA",
      Brand = "Prusament",
      Colour = "Orange"
    });
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

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
  public async Task CreateFilamentRequest_RequiresAuthentication()
  {
    // Arrange
    var createDto = new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "PLA",
      Brand = "Prusament",
      Colour = "Red"
    };

    // Clear authentication (guest session token)
    var unauthClient = Factory.CreateClient();

    // Act
    var response = await unauthClient.PostAsJsonAsync("/api/filamentrequests", createDto);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var error = await response.Content.ReadAsStringAsync();
    Assert.Contains("No user session found", error);
  }
}
