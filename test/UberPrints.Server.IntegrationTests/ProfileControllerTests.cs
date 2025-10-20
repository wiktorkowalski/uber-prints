using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class ProfileControllerTests : IntegrationTestBase
{
  public ProfileControllerTests(IntegrationTestFactory factory) : base(factory)
  {
  }

  #region GET /api/profile Tests

  [Fact]
  public async Task GetProfile_ReturnsProfile_WhenAuthenticatedUserExists()
  {
    // Arrange - Create authenticated user and get token
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "123456789",
        username: "testuser",
        globalName: "Test User",
        avatarHash: "abcdef123456",
        isAdmin: false
    );

    var authenticatedClient = CreateAuthenticatedClient(token);

    // Act
    var response = await authenticatedClient.GetAsync("/api/profile");

    // Assert
    response.EnsureSuccessStatusCode();
    var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();

    Assert.NotNull(profile);
    Assert.Equal(user.Id, profile.Id);
    Assert.Equal("123456789", profile.DiscordId);
    Assert.Equal("testuser", profile.Username);
    Assert.Equal("Test User", profile.GlobalName);
    Assert.Equal("abcdef123456", profile.AvatarHash);
    Assert.False(profile.IsAdmin);
    Assert.NotEqual(default, profile.CreatedAt);
  }

  [Fact]
  public async Task GetProfile_ReturnsCompleteProfileData_WithAllFields()
  {
    // Arrange - Create admin user with all fields populated
    var createdAt = DateTime.UtcNow.AddDays(-10);
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "987654321",
        username: "fulluser",
        globalName: "Full User Name",
        avatarHash: "xyz789abc",
        isAdmin: true
    );

    // Update created date manually
    using var scope = Factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbUser = await context.Users.FindAsync(user.Id);
    dbUser!.CreatedAt = createdAt;
    await context.SaveChangesAsync();

    var authenticatedClient = CreateAuthenticatedClient(token);

    // Act
    var response = await authenticatedClient.GetAsync("/api/profile");

    // Assert
    response.EnsureSuccessStatusCode();
    var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();

    Assert.NotNull(profile);
    Assert.Equal(user.Id, profile.Id);
    Assert.Equal("987654321", profile.DiscordId);
    Assert.Equal("fulluser", profile.Username);
    Assert.Equal("Full User Name", profile.GlobalName);
    Assert.Equal("xyz789abc", profile.AvatarHash);
    Assert.True(profile.IsAdmin);
  }

  [Fact]
  public async Task GetProfile_ComputesAvatarUrl_WhenDiscordIdAndHashPresent()
  {
    // Arrange
    var discordId = "555444333";
    var avatarHash = "abcdef123456";
    var expectedAvatarUrl = $"https://cdn.discordapp.com/avatars/{discordId}/{avatarHash}.png";

    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: discordId,
        username: "avataruser",
        globalName: "Avatar User",
        avatarHash: avatarHash
    );

    var authenticatedClient = CreateAuthenticatedClient(token);

    // Act
    var response = await authenticatedClient.GetAsync("/api/profile");

    // Assert
    response.EnsureSuccessStatusCode();
    var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();

    Assert.NotNull(profile);
    Assert.Equal(expectedAvatarUrl, profile.AvatarUrl);
  }

  [Fact]
  public async Task GetProfile_ReturnsNullAvatarUrl_WhenAvatarHashIsNull()
  {
    // Arrange - Create user without avatar
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "111222333",
        username: "noavatar",
        globalName: "No Avatar User",
        avatarHash: null
    );

    var authenticatedClient = CreateAuthenticatedClient(token);

    // Act
    var response = await authenticatedClient.GetAsync("/api/profile");

    // Assert
    response.EnsureSuccessStatusCode();
    var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();

    Assert.NotNull(profile);
    Assert.Null(profile.AvatarUrl);
  }

  [Fact]
  public async Task GetProfile_ReturnsUnauthorized_WhenNoJwtTokenProvided()
  {
    // Arrange - Create a client without authentication
    var unauthenticatedClient = Factory.CreateClient();

    // Act
    var response = await unauthenticatedClient.GetAsync("/api/profile");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetProfile_ReturnsNotFound_WhenUserDoesNotExistInDatabase()
  {
    // Arrange - Generate a token for a non-existent user
    using var scope = Factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Create user, get token, then delete user
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "999888777",
        username: "deleteduser"
    );

    // Delete the user from database
    var dbUser = await context.Users.FindAsync(user.Id);
    context.Users.Remove(dbUser!);
    await context.SaveChangesAsync();

    var authenticatedClient = CreateAuthenticatedClient(token);

    // Act
    var response = await authenticatedClient.GetAsync("/api/profile");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  #endregion

  #region PUT /api/profile/display-name Tests

  [Fact]
  public async Task UpdateDisplayName_UpdatesGlobalName_WhenValidDataProvided()
  {
    // Arrange - Create user
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "123456789",
        username: "testuser",
        globalName: "Original Name",
        avatarHash: "abcdef123456"
    );

    var authenticatedClient = CreateAuthenticatedClient(token);

    var updateDto = new UpdateDisplayNameDto
    {
      DisplayName = "New Display Name"
    };

    // Act
    var response = await authenticatedClient.PutAsJsonAsync("/api/profile/display-name", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();

    Assert.NotNull(profile);
    Assert.Equal("New Display Name", profile.GlobalName);

    // Verify it was actually updated in database
    using var scope = Factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbUser = await context.Users.FindAsync(user.Id);
    Assert.Equal("New Display Name", dbUser!.GlobalName);
  }

  [Fact]
  public async Task UpdateDisplayName_ReturnsUpdatedProfile_WithNewGlobalName()
  {
    // Arrange
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "987654321",
        username: "testuser2",
        globalName: "Old Name",
        avatarHash: null
    );

    var authenticatedClient = CreateAuthenticatedClient(token);

    var updateDto = new UpdateDisplayNameDto
    {
      DisplayName = "Updated Name"
    };

    // Act
    var response = await authenticatedClient.PutAsJsonAsync("/api/profile/display-name", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();

    Assert.NotNull(profile);
    Assert.Equal(user.Id, profile.Id);
    Assert.Equal("Updated Name", profile.GlobalName);
    Assert.Equal("987654321", profile.DiscordId);
    Assert.Equal("testuser2", profile.Username);
  }

  [Fact]
  public async Task UpdateDisplayName_PreservesOtherFields_WhenUpdatingDisplayName()
  {
    // Arrange
    var createdAt = DateTime.UtcNow.AddDays(-5);
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "555555555",
        username: "preservetest",
        globalName: "Original",
        avatarHash: "hash123",
        isAdmin: true
    );

    // Update created date
    using var scope1 = Factory.Services.CreateScope();
    var context1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbUser1 = await context1.Users.FindAsync(user.Id);
    dbUser1!.CreatedAt = createdAt;
    await context1.SaveChangesAsync();

    var authenticatedClient = CreateAuthenticatedClient(token);

    var updateDto = new UpdateDisplayNameDto
    {
      DisplayName = "Changed Name"
    };

    // Act
    var response = await authenticatedClient.PutAsJsonAsync("/api/profile/display-name", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();

    Assert.NotNull(profile);
    Assert.Equal("Changed Name", profile.GlobalName);
    // Verify other fields unchanged
    Assert.Equal("555555555", profile.DiscordId);
    Assert.Equal("preservetest", profile.Username);
    Assert.Equal("hash123", profile.AvatarHash);
    Assert.True(profile.IsAdmin);
  }

  [Fact]
  public async Task UpdateDisplayName_ReturnsUnauthorized_WhenNoJwtTokenProvided()
  {
    // Arrange - Create unauthenticated client
    var unauthenticatedClient = Factory.CreateClient();

    var updateDto = new UpdateDisplayNameDto
    {
      DisplayName = "Some Name"
    };

    // Act
    var response = await unauthenticatedClient.PutAsJsonAsync("/api/profile/display-name", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task UpdateDisplayName_ReturnsNotFound_WhenUserDoesNotExist()
  {
    // Arrange - Create user, get token, then delete user
    var (user, token) = await CreateAuthenticatedUserWithToken(
        discordId: "444333222",
        username: "deleteduser",
        globalName: "Will Be Deleted"
    );

    using var scope = Factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbUser = await context.Users.FindAsync(user.Id);
    context.Users.Remove(dbUser!);
    await context.SaveChangesAsync();

    var authenticatedClient = CreateAuthenticatedClient(token);

    var updateDto = new UpdateDisplayNameDto
    {
      DisplayName = "New Name"
    };

    // Act
    var response = await authenticatedClient.PutAsJsonAsync("/api/profile/display-name", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  #endregion

  #region Validation Tests

  [Fact]
  public async Task UpdateDisplayName_ReturnsBadRequest_WhenDisplayNameIsEmpty()
  {
    // Arrange
    var (user, token) = await CreateAuthenticatedUserWithToken();
    var authenticatedClient = CreateAuthenticatedClient(token);

    var updateDto = new UpdateDisplayNameDto
    {
      DisplayName = "" // Empty string - fails [Required] validation
    };

    // Act
    var response = await authenticatedClient.PutAsJsonAsync("/api/profile/display-name", updateDto);

    // Assert
    // [Required] validation should fail for empty strings
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UpdateDisplayName_ReturnsBadRequest_WhenDisplayNameExceedsMaxLength()
  {
    // Arrange
    var (user, token) = await CreateAuthenticatedUserWithToken();
    var authenticatedClient = CreateAuthenticatedClient(token);

    var updateDto = new UpdateDisplayNameDto
    {
      DisplayName = new string('a', 101) // 101 characters - exceeds [MaxLength(100)]
    };

    // Act
    var response = await authenticatedClient.PutAsJsonAsync("/api/profile/display-name", updateDto);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UpdateDisplayName_AcceptsMaxLengthDisplayName()
  {
    // Arrange
    var (user, token) = await CreateAuthenticatedUserWithToken();
    var authenticatedClient = CreateAuthenticatedClient(token);

    var updateDto = new UpdateDisplayNameDto
    {
      DisplayName = new string('a', 100) // Exactly 100 characters - at the limit
    };

    // Act
    var response = await authenticatedClient.PutAsJsonAsync("/api/profile/display-name", updateDto);

    // Assert
    response.EnsureSuccessStatusCode();
    var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
    Assert.NotNull(profile);
    Assert.Equal(100, profile.GlobalName!.Length);
  }

  #endregion
}
