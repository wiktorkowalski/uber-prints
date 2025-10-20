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

  // Note: Stock validation was intentionally removed from the code.
  // The system now allows requests with out-of-stock filaments (admin will assign filament later).
  // See CLAUDE.md: "Allow requests without filament - admin will assign one later"

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

  // Note: Stock validation was intentionally removed from the code. The system now allows
  // requests with out-of-stock filaments (admin will assign filament later).
  // See CLAUDE.md: "Allow requests without filament - admin will assign one later"

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

  #region Privacy Tests (IsPublic flag)

  [Fact]
  public async Task GetRequests_ReturnsOnlyPublicRequests_ForUnauthenticatedUsers()
  {
    // Arrange - Create filament
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    // Create public request
    var publicRequestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Public User");
    var publicResponse = await Client.PostAsJsonAsync("/api/requests", publicRequestDto);
    publicResponse.EnsureSuccessStatusCode();

    // Create private request with same guest session
    var privateRequestDto = new CreatePrintRequestDto
    {
      RequesterName = "Private User",
      ModelUrl = "https://example.com/private.stl",
      Notes = "Private request",
      RequestDelivery = true,
      IsPublic = false,
      FilamentId = filament.Id
    };
    var privateResponse = await Client.PostAsJsonAsync("/api/requests", privateRequestDto);
    privateResponse.EnsureSuccessStatusCode();

    // Create unauthenticated client (no guest session, no JWT)
    var unauthenticatedClient = Factory.CreateClient();

    // Act
    var response = await unauthenticatedClient.GetAsync("/api/requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<PrintRequestDto>>();
    Assert.NotNull(requests);
    // Should only see public request, not the private one
    Assert.Single(requests);
    Assert.Equal("Public User", requests[0].RequesterName);
  }

  [Fact]
  public async Task GetRequests_ReturnsPublicPlusOwnPrivateRequests_ForGuestUser()
  {
    // Arrange - Create filament
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    // Guest creates own private request
    var ownPrivateDto = new CreatePrintRequestDto
    {
      RequesterName = "Own Private",
      ModelUrl = "https://example.com/own.stl",
      Notes = "My private request",
      RequestDelivery = true,
      IsPublic = false,
      FilamentId = filament.Id
    };
    await Client.PostAsJsonAsync("/api/requests", ownPrivateDto);

    // Guest creates public request
    var publicDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Public Request");
    await Client.PostAsJsonAsync("/api/requests", publicDto);

    // Another guest creates a private request
    var otherGuestResponse = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var otherGuestResult = await otherGuestResponse.Content.ReadFromJsonAsync<GuestSessionResponse>();
    var otherGuestClient = Factory.CreateClient();
    otherGuestClient.DefaultRequestHeaders.Add("X-Guest-Session-Token", otherGuestResult!.guestSessionToken);

    var otherPrivateDto = new CreatePrintRequestDto
    {
      RequesterName = "Other Private",
      ModelUrl = "https://example.com/other.stl",
      Notes = "Other's private request",
      RequestDelivery = true,
      IsPublic = false,
      FilamentId = filament.Id
    };
    await otherGuestClient.PostAsJsonAsync("/api/requests", otherPrivateDto);

    // Act - Get requests as original guest
    var response = await Client.GetAsync("/api/requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<PrintRequestDto>>();
    Assert.NotNull(requests);
    // Should see own private + public, but not other's private
    Assert.Equal(2, requests.Count);
    Assert.Contains(requests, r => r.RequesterName == "Own Private");
    Assert.Contains(requests, r => r.RequesterName == "Public Request");
    Assert.DoesNotContain(requests, r => r.RequesterName == "Other Private");
  }

  [Fact]
  public async Task GetRequests_ReturnsPublicPlusOwnPrivateRequests_ForAuthenticatedUser()
  {
    // Arrange - Create filament
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    // Create authenticated user 1
    var (user1, token1) = await CreateAuthenticatedUserWithToken(
        discordId: "111111111",
        username: "user1"
    );
    var client1 = CreateAuthenticatedClient(token1);
    client1.DefaultRequestHeaders.Add("X-Guest-Session-Token", GuestSessionToken!);

    // User 1 creates private request
    var user1PrivateDto = new CreatePrintRequestDto
    {
      RequesterName = "User 1 Private",
      ModelUrl = "https://example.com/user1.stl",
      Notes = "User 1's private",
      RequestDelivery = true,
      IsPublic = false,
      FilamentId = filament.Id
    };
    await client1.PostAsJsonAsync("/api/requests", user1PrivateDto);

    // Guest creates public request
    var publicDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Public Request");
    await Client.PostAsJsonAsync("/api/requests", publicDto);

    // Create authenticated user 2
    var guestResponse2 = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var guestResult2 = await guestResponse2.Content.ReadFromJsonAsync<GuestSessionResponse>();

    var (user2, token2) = await CreateAuthenticatedUserWithToken(
        discordId: "222222222",
        username: "user2"
    );
    var client2 = CreateAuthenticatedClient(token2);
    client2.DefaultRequestHeaders.Add("X-Guest-Session-Token", guestResult2!.guestSessionToken);

    var user2PrivateDto = new CreatePrintRequestDto
    {
      RequesterName = "User 2 Private",
      ModelUrl = "https://example.com/user2.stl",
      Notes = "User 2's private",
      RequestDelivery = true,
      IsPublic = false,
      FilamentId = filament.Id
    };
    await client2.PostAsJsonAsync("/api/requests", user2PrivateDto);

    // Act - Get requests as user 1
    var response = await client1.GetAsync("/api/requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var requests = await response.Content.ReadFromJsonAsync<List<PrintRequestDto>>();
    Assert.NotNull(requests);
    // Should see own private + public, but not user 2's private
    Assert.Equal(2, requests.Count);
    Assert.Contains(requests, r => r.RequesterName == "User 1 Private");
    Assert.Contains(requests, r => r.RequesterName == "Public Request");
    Assert.DoesNotContain(requests, r => r.RequesterName == "User 2 Private");
  }

  [Fact]
  public async Task GetRequestById_ReturnsPublicRequest_ToAnyUser()
  {
    // Arrange - Create public request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var publicDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Public Request");
    var createResponse = await Client.PostAsJsonAsync("/api/requests", publicDto);
    var publicRequest = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(publicRequest);

    // Create unauthenticated client
    var unauthenticatedClient = Factory.CreateClient();

    // Act
    var response = await unauthenticatedClient.GetAsync($"/api/requests/{publicRequest.Id}");

    // Assert
    response.EnsureSuccessStatusCode();
    var request = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);
    Assert.Equal(publicRequest.Id, request.Id);
  }

  [Fact]
  public async Task GetRequestById_ReturnsPrivateRequest_ToOwner()
  {
    // Arrange - Create private request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var privateDto = new CreatePrintRequestDto
    {
      RequesterName = "Private Owner",
      ModelUrl = "https://example.com/private.stl",
      Notes = "Private request",
      RequestDelivery = true,
      IsPublic = false,
      FilamentId = filament.Id
    };
    var createResponse = await Client.PostAsJsonAsync("/api/requests", privateDto);
    var privateRequest = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(privateRequest);

    // Act - Owner tries to view own private request
    var response = await Client.GetAsync($"/api/requests/{privateRequest.Id}");

    // Assert
    response.EnsureSuccessStatusCode();
    var request = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);
    Assert.Equal(privateRequest.Id, request.Id);
    Assert.False(request.IsPublic);
  }

  [Fact]
  public async Task GetRequestById_ReturnsNotFound_WhenAccessingOthersPrivateRequest()
  {
    // Arrange - Guest 1 creates private request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var privateDto = new CreatePrintRequestDto
    {
      RequesterName = "User 1 Private",
      ModelUrl = "https://example.com/private.stl",
      Notes = "Private request",
      RequestDelivery = true,
      IsPublic = false,
      FilamentId = filament.Id
    };
    var createResponse = await Client.PostAsJsonAsync("/api/requests", privateDto);
    var privateRequest = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(privateRequest);

    // Create another guest
    var otherGuestResponse = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var otherGuestResult = await otherGuestResponse.Content.ReadFromJsonAsync<GuestSessionResponse>();
    var otherGuestClient = Factory.CreateClient();
    otherGuestClient.DefaultRequestHeaders.Add("X-Guest-Session-Token", otherGuestResult!.guestSessionToken);

    // Act - Other guest tries to view private request
    var response = await otherGuestClient.GetAsync($"/api/requests/{privateRequest.Id}");

    // Assert
    // Returns NotFound (not Forbidden) to avoid information disclosure
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetRequestById_ReturnsNotFound_WhenUnauthenticatedAccessingPrivateRequest()
  {
    // Arrange - Create private request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var privateDto = new CreatePrintRequestDto
    {
      RequesterName = "Private User",
      ModelUrl = "https://example.com/private.stl",
      Notes = "Private request",
      RequestDelivery = true,
      IsPublic = false,
      FilamentId = filament.Id
    };
    var createResponse = await Client.PostAsJsonAsync("/api/requests", privateDto);
    var privateRequest = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(privateRequest);

    // Create unauthenticated client
    var unauthenticatedClient = Factory.CreateClient();

    // Act
    var response = await unauthenticatedClient.GetAsync($"/api/requests/{privateRequest.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  #endregion

  #region Ownership Validation Tests

  [Fact]
  public async Task UpdateRequest_ReturnsForbid_WhenAuthenticatedUserDoesNotOwnRequest()
  {
    // Arrange - User 1 creates a request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var (user1, token1) = await CreateAuthenticatedUserWithToken(
        discordId: "111111111",
        username: "user1"
    );
    var client1 = CreateAuthenticatedClient(token1);
    client1.DefaultRequestHeaders.Add("X-Guest-Session-Token", GuestSessionToken!);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "User 1 Request");
    var createResponse = await client1.PostAsJsonAsync("/api/requests", requestDto);
    var request = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);

    // User 2 tries to update user 1's request
    var guestResponse2 = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var guestResult2 = await guestResponse2.Content.ReadFromJsonAsync<GuestSessionResponse>();

    var (user2, token2) = await CreateAuthenticatedUserWithToken(
        discordId: "222222222",
        username: "user2"
    );
    var client2 = CreateAuthenticatedClient(token2);
    client2.DefaultRequestHeaders.Add("X-Guest-Session-Token", guestResult2!.guestSessionToken);

    var updateDto = new UpdatePrintRequestDto
    {
      RequesterName = "Hacked Name",
      ModelUrl = request.ModelUrl,
      Notes = request.Notes,
      RequestDelivery = request.RequestDelivery,
      IsPublic = request.IsPublic,
      FilamentId = request.FilamentId
    };

    // Act
    var response = await client2.PutAsJsonAsync($"/api/requests/{request.Id}", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task UpdateRequest_ReturnsForbid_WhenGuestDoesNotOwnRequest()
  {
    // Arrange - Guest 1 creates a request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Guest 1 Request");
    var createResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var request = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);

    // Guest 2 tries to update guest 1's request
    var otherGuestResponse = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var otherGuestResult = await otherGuestResponse.Content.ReadFromJsonAsync<GuestSessionResponse>();
    var otherGuestClient = Factory.CreateClient();
    otherGuestClient.DefaultRequestHeaders.Add("X-Guest-Session-Token", otherGuestResult!.guestSessionToken);

    var updateDto = new UpdatePrintRequestDto
    {
      RequesterName = "Hacked Name",
      ModelUrl = request.ModelUrl,
      Notes = request.Notes,
      RequestDelivery = request.RequestDelivery,
      IsPublic = request.IsPublic,
      FilamentId = request.FilamentId
    };

    // Act
    var response = await otherGuestClient.PutAsJsonAsync($"/api/requests/{request.Id}", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task UpdateRequest_ReturnsUnauthorized_WhenNoAuthenticationProvided()
  {
    // Arrange - Create a request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Test Request");
    var createResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var request = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);

    // Create unauthenticated client
    var unauthenticatedClient = Factory.CreateClient();

    var updateDto = new UpdatePrintRequestDto
    {
      RequesterName = "Updated Name",
      ModelUrl = request.ModelUrl,
      Notes = request.Notes,
      RequestDelivery = request.RequestDelivery,
      IsPublic = request.IsPublic,
      FilamentId = request.FilamentId
    };

    // Act
    var response = await unauthenticatedClient.PutAsJsonAsync($"/api/requests/{request.Id}", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task DeleteRequest_ReturnsForbid_WhenAuthenticatedUserDoesNotOwnRequest()
  {
    // Arrange - User 1 creates a request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var (user1, token1) = await CreateAuthenticatedUserWithToken(
        discordId: "333333333",
        username: "user1"
    );
    var client1 = CreateAuthenticatedClient(token1);
    client1.DefaultRequestHeaders.Add("X-Guest-Session-Token", GuestSessionToken!);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "User 1 Request");
    var createResponse = await client1.PostAsJsonAsync("/api/requests", requestDto);
    var request = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);

    // User 2 tries to delete user 1's request
    var guestResponse2 = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var guestResult2 = await guestResponse2.Content.ReadFromJsonAsync<GuestSessionResponse>();

    var (user2, token2) = await CreateAuthenticatedUserWithToken(
        discordId: "444444444",
        username: "user2"
    );
    var client2 = CreateAuthenticatedClient(token2);
    client2.DefaultRequestHeaders.Add("X-Guest-Session-Token", guestResult2!.guestSessionToken);

    // Act
    var response = await client2.DeleteAsync($"/api/requests/{request.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteRequest_ReturnsForbid_WhenGuestDoesNotOwnRequest()
  {
    // Arrange - Guest 1 creates a request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Guest 1 Request");
    var createResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var request = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);

    // Guest 2 tries to delete guest 1's request
    var otherGuestResponse = await Factory.CreateClient().PostAsync("/api/auth/guest", null);
    var otherGuestResult = await otherGuestResponse.Content.ReadFromJsonAsync<GuestSessionResponse>();
    var otherGuestClient = Factory.CreateClient();
    otherGuestClient.DefaultRequestHeaders.Add("X-Guest-Session-Token", otherGuestResult!.guestSessionToken);

    // Act
    var response = await otherGuestClient.DeleteAsync($"/api/requests/{request.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteRequest_ReturnsUnauthorized_WhenNoAuthenticationProvided()
  {
    // Arrange - Create a request
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Test Request");
    var createResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var request = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);

    // Create unauthenticated client
    var unauthenticatedClient = Factory.CreateClient();

    // Act
    var response = await unauthenticatedClient.DeleteAsync($"/api/requests/{request.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  #endregion

  #region Optional Filament Tests

  [Fact]
  public async Task CreateRequest_SucceedsWithNullFilamentId()
  {
    // Arrange
    var createDto = new CreatePrintRequestDto
    {
      RequesterName = "Test User",
      ModelUrl = "https://example.com/model.stl",
      Notes = "No filament specified yet",
      RequestDelivery = true,
      IsPublic = true,
      FilamentId = null // No filament selected
    };

    // Act
    var response = await Client.PostAsJsonAsync("/api/requests", createDto);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var request = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);
    Assert.Null(request.FilamentId);
    Assert.Null(request.FilamentName);
    Assert.Equal(RequestStatusEnum.Pending, request.CurrentStatus);
  }

  [Fact]
  public async Task UpdateRequest_CanRemoveFilament()
  {
    // Arrange - Create request with filament
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var createDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Test User");
    var createResponse = await Client.PostAsJsonAsync("/api/requests", createDto);
    var request = await createResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(request);
    Assert.NotNull(request.FilamentId);

    // Update to remove filament
    var updateDto = new UpdatePrintRequestDto
    {
      RequesterName = request.RequesterName,
      ModelUrl = request.ModelUrl,
      Notes = request.Notes,
      RequestDelivery = request.RequestDelivery,
      IsPublic = request.IsPublic,
      FilamentId = null // Remove filament
    };

    // Act
    var response = await Client.PutAsJsonAsync($"/api/requests/{request.Id}", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var updatedRequest = await response.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(updatedRequest);
    Assert.Null(updatedRequest.FilamentId);
    Assert.Null(updatedRequest.FilamentName);
  }

  #endregion

  private record GuestSessionResponse(string guestSessionToken, string username);
}
