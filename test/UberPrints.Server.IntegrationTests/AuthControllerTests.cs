using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class AuthControllerTests : IntegrationTestBase
{
  public AuthControllerTests(IntegrationTestFactory factory) : base(factory)
  {
  }

  [Fact]
  public async Task CreateGuestSession_ReturnsGuestSessionToken()
  {
    // Act
    var response = await Client.PostAsync("/api/auth/guest", null);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<GuestSessionResponse>();
    Assert.NotNull(result);
    Assert.NotNull(result.GuestSessionToken);
    Assert.NotEmpty(result.GuestSessionToken);
    Assert.NotEqual(Guid.Empty, result.UserId);
  }

  [Fact]
  public async Task CreateGuestSession_CreatesUserInDatabase()
  {
    // Act
    var response = await Client.PostAsync("/api/auth/guest", null);
    var result = await response.Content.ReadFromJsonAsync<GuestSessionResponse>();
    Assert.NotNull(result);

    // Assert - verify user exists in database
    using var scope = Factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var user = await context.Users.FindAsync(result.UserId);
    Assert.NotNull(user);
    Assert.Equal(result.GuestSessionToken, user.GuestSessionToken);
    Assert.Null(user.DiscordId);
    Assert.False(user.IsAdmin);
    Assert.StartsWith("Guest_", user.Username);
  }

  [Fact]
  public async Task CreateGuestSession_GeneratesUniqueTokens()
  {
    // Act - create two guest sessions
    var response1 = await Client.PostAsync("/api/auth/guest", null);
    var response2 = await Client.PostAsync("/api/auth/guest", null);

    // Assert
    response1.EnsureSuccessStatusCode();
    response2.EnsureSuccessStatusCode();

    var result1 = await response1.Content.ReadFromJsonAsync<GuestSessionResponse>();
    var result2 = await response2.Content.ReadFromJsonAsync<GuestSessionResponse>();

    Assert.NotNull(result1);
    Assert.NotNull(result2);
    Assert.NotEqual(result1.GuestSessionToken, result2.GuestSessionToken);
    Assert.NotEqual(result1.UserId, result2.UserId);
  }

  [Fact]
  public async Task CreateGuestSession_AllowsGuestToCreatePrintRequest()
  {
    // Arrange - create guest session
    var guestResponse = await Client.PostAsync("/api/auth/guest", null);
    var guestResult = await guestResponse.Content.ReadFromJsonAsync<GuestSessionResponse>();
    Assert.NotNull(guestResult);

    // Create filament
    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    // Act - create print request as guest
    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Guest User");
    var requestResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);

    // Assert
    requestResponse.EnsureSuccessStatusCode();
    var createdRequest = await requestResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(createdRequest);
    Assert.Equal("Guest User", createdRequest.RequesterName);
    Assert.NotNull(createdRequest.GuestTrackingToken);
  }

  [Fact]
  public async Task GuestSession_CanTrackTheirRequests()
  {
    // Arrange - create guest and print request
    var guestResponse = await Client.PostAsync("/api/auth/guest", null);
    var guestResult = await guestResponse.Content.ReadFromJsonAsync<GuestSessionResponse>();
    Assert.NotNull(guestResult);

    var filamentDto = TestDataFactory.CreateFilamentDto();
    var filamentResponse = await Client.PostAsJsonAsync("/api/admin/filaments", filamentDto);
    var filament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
    Assert.NotNull(filament);

    var requestDto = TestDataFactory.CreatePrintRequestDto(filament.Id, "Test Guest");
    var requestResponse = await Client.PostAsJsonAsync("/api/requests", requestDto);
    var createdRequest = await requestResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(createdRequest);

    // Act - track request using guest tracking token
    var trackResponse = await Client.GetAsync($"/api/requests/track/{createdRequest.GuestTrackingToken}");

    // Assert
    trackResponse.EnsureSuccessStatusCode();
    var trackedRequest = await trackResponse.Content.ReadFromJsonAsync<PrintRequestDto>();
    Assert.NotNull(trackedRequest);
    Assert.Equal(createdRequest.Id, trackedRequest.Id);
    Assert.Equal("Test Guest", trackedRequest.RequesterName);
  }

  [Fact]
  public async Task Login_ReturnsErrorOrRedirect_WhenDirectlyAccessed()
  {
    // Note: Login endpoint requires actual Discord OAuth flow
    // Direct access without proper configuration will fail
    // This test verifies the endpoint exists

    // Act
    var response = await Client.GetAsync("/api/auth/login");

    // Assert
    // We expect some kind of error response since Discord OAuth isn't configured
    // In test environment, NotFound, BadRequest, InternalServerError, or Redirect are all acceptable
    Assert.False(response.IsSuccessStatusCode, "Login should not succeed without proper OAuth configuration");
  }

  [Fact]
  public async Task Logout_ReturnsSuccess()
  {
    // Act
    var response = await Client.PostAsync("/api/auth/logout", null);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<LogoutResponse>();
    Assert.NotNull(result);
    Assert.Equal("Logged out successfully", result.Message);
  }

  [Fact]
  public async Task GetCurrentUser_ReturnsNotFound_WhenNotAuthenticated()
  {
    // Act
    var response = await Client.GetAsync("/api/auth/me");

    // Assert
    // When not authenticated, the endpoint returns NotFound since it can't find the user
    // This is expected behavior in the current implementation
    Assert.True(response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task CreateGuestSession_CreatesUserWith32CharacterToken()
  {
    // Act
    var response = await Client.PostAsync("/api/auth/guest", null);
    var result = await response.Content.ReadFromJsonAsync<GuestSessionResponse>();

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.GuestSessionToken);
    Assert.Equal(32, result.GuestSessionToken.Length);

    // Should be uppercase hex (GUID without dashes)
    Assert.Matches("^[A-F0-9]{32}$", result.GuestSessionToken);
  }

  [Fact]
  public async Task CreateGuestSession_MultipleConcurrentRequests_AllSucceed()
  {
    // Arrange - create 10 concurrent guest session requests
    var tasks = Enumerable.Range(0, 10)
        .Select(_ => Client.PostAsync("/api/auth/guest", null))
        .ToList();

    // Act
    var responses = await Task.WhenAll(tasks);

    // Assert - all should succeed
    Assert.All(responses, r => r.EnsureSuccessStatusCode());

    var results = await Task.WhenAll(
        responses.Select(r => r.Content.ReadFromJsonAsync<GuestSessionResponse>())
    );

    // All tokens should be unique
    var tokens = results.Select(r => r!.GuestSessionToken).ToList();
    Assert.Equal(10, tokens.Distinct().Count());

    // All user IDs should be unique
    var userIds = results.Select(r => r!.UserId).ToList();
    Assert.Equal(10, userIds.Distinct().Count());
  }

  // Helper classes for deserializing responses
  private class GuestSessionResponse
  {
    public string GuestSessionToken { get; set; } = string.Empty;
    public Guid UserId { get; set; }
  }

  private class LogoutResponse
  {
    public string Message { get; set; } = string.Empty;
  }
}
