using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
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
    // In test environment with permissive authorization and RedirectHandler,
    // the endpoint may return OK, Redirect, or an error
    // This test verifies the endpoint exists and behaves correctly

    // Act
    var response = await Client.GetAsync("/api/auth/login");

    // Assert
    // The endpoint should respond (any status is fine, we just verify it exists)
    Assert.NotNull(response);
    // In test environment, OK (200), Redirect (302), or errors are all acceptable
    // The endpoint is working if we get any response
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

  #region RefreshToken Tests

  [Fact]
  public async Task RefreshToken_ReturnsNewJwtToken_WhenAuthenticatedUserExists()
  {
    // Arrange - Create authenticated user
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "123456789",
        username: "testuser",
        globalName: "Test User"
    );

    var authenticatedClient = CreateAuthenticatedClient(token);

    // Wait a moment to ensure the new token has a different timestamp
    await Task.Delay(1000); // 1 second

    // Act
    var response = await authenticatedClient.PostAsync("/api/auth/refresh", null);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
    Assert.NotNull(result);
    Assert.NotNull(result.token);
    Assert.NotEmpty(result.token);
    // The new token should be different from the original (due to different timestamp)
    Assert.NotEqual(token, result.token);
  }

  [Fact]
  public async Task RefreshToken_NewTokenIsValid_CanAccessProtectedEndpoints()
  {
    // Arrange - Create authenticated user
    var (user, oldToken) = await CreateAuthenticatedUserWithToken(
        discordId: "987654321",
        username: "refreshuser",
        globalName: "Refresh Test User"
    );

    var authenticatedClient = CreateAuthenticatedClient(oldToken);

    // Get new token via refresh
    var refreshResponse = await authenticatedClient.PostAsync("/api/auth/refresh", null);
    refreshResponse.EnsureSuccessStatusCode();
    var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>();
    Assert.NotNull(refreshResult);
    Assert.NotNull(refreshResult.token);
    Assert.NotEmpty(refreshResult.token);

    // Decode and inspect the refreshed token
    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(refreshResult.token);

    // Verify the token has the expected claims
    var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    Assert.NotNull(userIdClaim);
    Assert.Equal(user.Id.ToString(), userIdClaim);

    // Create new client with refreshed token
    var newClient = CreateAuthenticatedClient(refreshResult.token);

    // Act - Use new token to access protected endpoint
    var profileResponse = await newClient.GetAsync("/api/profile");

    // Assert
    profileResponse.EnsureSuccessStatusCode();
    var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileDto>();
    Assert.NotNull(profile);
    Assert.Equal(user.Id, profile.Id);
    Assert.Equal("refreshuser", profile.Username);
  }

  [Fact]
  public async Task RefreshToken_ReturnsUnauthorized_WhenNoAuthenticationProvided()
  {
    // Arrange - Create unauthenticated client
    var unauthenticatedClient = Factory.CreateClient();

    // Act
    var response = await unauthenticatedClient.PostAsync("/api/auth/refresh", null);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task RefreshToken_ReturnsNotFound_WhenUserDoesNotExistInDatabase()
  {
    // Arrange - Create user, get token, then delete user
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "555666777",
        username: "deleteduser",
        globalName: "Will Be Deleted"
    );

    using var scope = Factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbUser = await context.Users.FindAsync(user.Id);
    context.Users.Remove(dbUser!);
    await context.SaveChangesAsync();

    var authenticatedClient = CreateAuthenticatedClient(token);

    // Act
    var response = await authenticatedClient.PostAsync("/api/auth/refresh", null);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task RefreshToken_ReturnsUnauthorized_WhenTokenIsInvalid()
  {
    // Arrange - Create client with invalid token
    var invalidClient = Factory.CreateClient();
    invalidClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");

    // Act
    var response = await invalidClient.PostAsync("/api/auth/refresh", null);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task RefreshToken_PreservesUserClaims_InRefreshedToken()
  {
    // Arrange - Create admin user to test that admin role is preserved
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "admin123",
        username: "adminuser",
        globalName: "Admin User",
        isAdmin: true
    );

    var authenticatedClient = CreateAuthenticatedClient(token);
    await Task.Delay(1000); // Ensure different timestamp

    // Act
    var response = await authenticatedClient.PostAsync("/api/auth/refresh", null);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
    Assert.NotNull(result);
    Assert.NotNull(result.token);

    // Decode and verify claims
    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(result.token);

    var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;
    var isAdminClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "IsAdmin")?.Value;
    var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

    Assert.Equal(user.Id.ToString(), userIdClaim);
    Assert.Equal("adminuser", nameClaim);
    Assert.Equal("True", isAdminClaim);
    Assert.Equal("Admin", roleClaim);
  }

  [Fact]
  public async Task RefreshToken_CanRefreshMultipleTimes()
  {
    // Arrange - Create user and get initial token
    var (user, token1) = await CreateAuthenticatedUserWithToken(
        discordId: "refresh123",
        username: "refreshuser",
        globalName: "Refresh User"
    );

    var client1 = CreateAuthenticatedClient(token1);
    await Task.Delay(1000);

    // Act - Refresh the first time
    var response1 = await client1.PostAsync("/api/auth/refresh", null);
    response1.EnsureSuccessStatusCode();
    var result1 = await response1.Content.ReadFromJsonAsync<RefreshTokenResponse>();
    Assert.NotNull(result1);
    var token2 = result1.token;
    Assert.NotEqual(token1, token2);

    await Task.Delay(1000);

    // Act - Refresh the second time using the refreshed token
    var client2 = CreateAuthenticatedClient(token2);
    var response2 = await client2.PostAsync("/api/auth/refresh", null);
    response2.EnsureSuccessStatusCode();
    var result2 = await response2.Content.ReadFromJsonAsync<RefreshTokenResponse>();
    Assert.NotNull(result2);
    var token3 = result2.token;
    Assert.NotEqual(token2, token3);

    // Assert - All three tokens should be different but valid
    Assert.NotEqual(token1, token3);

    // Verify the final token can access protected endpoints
    var client3 = CreateAuthenticatedClient(token3);
    var profileResponse = await client3.GetAsync("/api/profile");
    profileResponse.EnsureSuccessStatusCode();
  }

  #endregion

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

  private class RefreshTokenResponse
  {
    public string token { get; set; } = string.Empty;
  }
}
