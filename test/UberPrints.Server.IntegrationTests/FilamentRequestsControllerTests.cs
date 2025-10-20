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

  #region Ownership Validation Tests

  [Fact]
  public async Task DeleteFilamentRequest_SucceedsWhenOwnerDeletes()
  {
    // Arrange - Guest creates a filament request
    var createDto = new CreateFilamentRequestDto
    {
      RequesterName = "Guest Owner",
      Material = "PLA",
      Brand = "Test Brand",
      Colour = "Blue"
    };
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", createDto);
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    // Act - Owner deletes their own request
    var response = await Client.DeleteAsync($"/api/filamentrequests/{createdRequest.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    // Verify deletion
    var getResponse = await Client.GetAsync($"/api/filamentrequests/{createdRequest.Id}");
    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
  }

  [Fact]
  public async Task DeleteFilamentRequest_ReturnsForbid_WhenAuthenticatedUserDoesNotOwn()
  {
    // Arrange - User 1 creates a filament request
    var (user1, token1) = await CreateAuthenticatedUserWithToken(
        discordId: "111111111",
        username: "user1"
    );
    var client1 = CreateAuthenticatedClient(token1);
    client1.DefaultRequestHeaders.Add("X-Guest-Session-Token", GuestSessionToken!);

    var createDto = new CreateFilamentRequestDto
    {
      RequesterName = "User 1",
      Material = "PLA",
      Brand = "Brand A",
      Colour = "Red"
    };
    var createResponse = await client1.PostAsJsonAsync("/api/filamentrequests", createDto);
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    // Create User 2
    var guestResponse2 = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var guestResult2 = await guestResponse2.Content.ReadFromJsonAsync<GuestSessionResponse>();

    var (user2, token2) = await CreateAuthenticatedUserWithToken(
        discordId: "222222222",
        username: "user2"
    );
    var client2 = CreateAuthenticatedClient(token2);
    client2.DefaultRequestHeaders.Add("X-Guest-Session-Token", guestResult2!.guestSessionToken);

    // Act - User 2 tries to delete User 1's request
    var response = await client2.DeleteAsync($"/api/filamentrequests/{createdRequest.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteFilamentRequest_ReturnsForbid_WhenGuestDoesNotOwn()
  {
    // Arrange - Guest 1 creates a filament request
    var createDto = new CreateFilamentRequestDto
    {
      RequesterName = "Guest 1",
      Material = "PETG",
      Brand = "Brand B",
      Colour = "Green"
    };
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", createDto);
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    // Create Guest 2
    var otherGuestResponse = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var otherGuestResult = await otherGuestResponse.Content.ReadFromJsonAsync<GuestSessionResponse>();
    var otherGuestClient = Factory.CreateClient();
    otherGuestClient.DefaultRequestHeaders.Add("X-Guest-Session-Token", otherGuestResult!.guestSessionToken);

    // Act - Guest 2 tries to delete Guest 1's request
    var response = await otherGuestClient.DeleteAsync($"/api/filamentrequests/{createdRequest.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteFilamentRequest_ReturnsUnauthorized_WhenNoAuthentication()
  {
    // Arrange - Create a filament request
    var createDto = new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "ABS",
      Brand = "Brand C",
      Colour = "White"
    };
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", createDto);
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    // Create unauthenticated client
    var unauthClient = Factory.CreateClient();

    // Act - Unauthenticated user tries to delete
    var response = await unauthClient.DeleteAsync($"/api/filamentrequests/{createdRequest.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task DeleteFilamentRequest_ReturnsUnauthorized_WhenInvalidGuestToken()
  {
    // Arrange - Create a filament request
    var createDto = new CreateFilamentRequestDto
    {
      RequesterName = "Test User",
      Material = "PLA",
      Brand = "Brand D",
      Colour = "Black"
    };
    var createResponse = await Client.PostAsJsonAsync("/api/filamentrequests", createDto);
    var createdRequest = await createResponse.Content.ReadFromJsonAsync<FilamentRequestDto>();
    Assert.NotNull(createdRequest);

    // Create client with invalid guest token
    var invalidClient = Factory.CreateClient();
    invalidClient.DefaultRequestHeaders.Add("X-Guest-Session-Token", "INVALIDTOKEN12345678901234567890");

    // Act
    var response = await invalidClient.DeleteAsync($"/api/filamentrequests/{createdRequest.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task DeleteFilamentRequest_ReturnsNotFound_WhenRequestDoesNotExist()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await Client.DeleteAsync($"/api/filamentrequests/{nonExistentId}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetMyFilamentRequests_ReturnsOnlyOwnRequests()
  {
    // Arrange - Guest 1 creates two requests
    await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Guest 1 Request 1",
      Material = "PLA",
      Brand = "Brand A",
      Colour = "Red"
    });

    await Client.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Guest 1 Request 2",
      Material = "PETG",
      Brand = "Brand B",
      Colour = "Blue"
    });

    // Guest 2 creates one request
    var otherGuestResponse = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var otherGuestResult = await otherGuestResponse.Content.ReadFromJsonAsync<GuestSessionResponse>();
    var otherGuestClient = Factory.CreateClient();
    otherGuestClient.DefaultRequestHeaders.Add("X-Guest-Session-Token", otherGuestResult!.guestSessionToken);

    await otherGuestClient.PostAsJsonAsync("/api/filamentrequests", new CreateFilamentRequestDto
    {
      RequesterName = "Guest 2 Request",
      Material = "ABS",
      Brand = "Brand C",
      Colour = "Green"
    });

    // Act - Get Guest 1's requests
    var response = await Client.GetAsync("/api/filamentrequests/my-requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<FilamentRequestDto>>();
    Assert.NotNull(requests);
    Assert.Equal(2, requests.Count);
    Assert.All(requests, r => Assert.StartsWith("Guest 1", r.RequesterName));
    Assert.DoesNotContain(requests, r => r.RequesterName == "Guest 2 Request");
  }

  [Fact]
  public async Task GetMyFilamentRequests_ReturnsUnauthorized_WhenNoAuthentication()
  {
    // Arrange - Create unauthenticated client
    var unauthClient = Factory.CreateClient();

    // Act
    var response = await unauthClient.GetAsync("/api/filamentrequests/my-requests");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetMyFilamentRequests_ReturnsEmptyList_WhenUserHasNoRequests()
  {
    // Arrange - Create a new guest session (no requests yet)
    var newGuestResponse = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var newGuestResult = await newGuestResponse.Content.ReadFromJsonAsync<GuestSessionResponse>();
    var newGuestClient = Factory.CreateClient();
    newGuestClient.DefaultRequestHeaders.Add("X-Guest-Session-Token", newGuestResult!.guestSessionToken);

    // Act
    var response = await newGuestClient.GetAsync("/api/filamentrequests/my-requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<FilamentRequestDto>>();
    Assert.NotNull(requests);
    Assert.Empty(requests);
  }

  #endregion

  private record GuestSessionResponse(string guestSessionToken, string username);
}
