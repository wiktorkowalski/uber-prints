using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Controllers;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;

namespace UberPrints.Server.UnitTests;

public class AuthControllerTests : TestBase
{
  [Fact]
  public async Task CreateGuestSession_CreatesGuestUser_AndReturnsTokenAndUserId()
  {
    // Act
    var result = await AuthController.CreateGuestSession();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = okResult.Value;

    Assert.NotNull(response);

    // Use reflection to get the properties from anonymous type
    var guestSessionToken = response.GetType().GetProperty("guestSessionToken")?.GetValue(response, null) as string;
    var userId = response.GetType().GetProperty("userId")?.GetValue(response, null);

    Assert.NotNull(guestSessionToken);
    Assert.NotNull(userId);
    Assert.NotEmpty(guestSessionToken);

    // Verify guest user was saved to database
    var savedUser = await Context.Users.FirstOrDefaultAsync(u => u.GuestSessionToken == guestSessionToken);
    Assert.NotNull(savedUser);
    Assert.Equal(guestSessionToken, savedUser.GuestSessionToken);
    Assert.Null(savedUser.DiscordId);
    Assert.False(savedUser.IsAdmin);
    Assert.StartsWith("Guest_", savedUser.Username);
  }

  [Fact]
  public async Task CreateGuestSession_GeneratesUniqueTokens_ForMultipleGuests()
  {
    // Act
    var result1 = await AuthController.CreateGuestSession();
    var result2 = await AuthController.CreateGuestSession();

    // Assert
    var okResult1 = Assert.IsType<OkObjectResult>(result1);
    var okResult2 = Assert.IsType<OkObjectResult>(result2);

    var response1 = okResult1.Value;
    var response2 = okResult2.Value;

    var token1 = response1?.GetType().GetProperty("guestSessionToken")?.GetValue(response1, null) as string;
    var token2 = response2?.GetType().GetProperty("guestSessionToken")?.GetValue(response2, null) as string;

    Assert.NotNull(token1);
    Assert.NotNull(token2);
    Assert.NotEqual(token1, token2);

    // Verify both users exist in database
    var users = await Context.Users
        .Where(u => u.GuestSessionToken == token1 || u.GuestSessionToken == token2)
        .ToListAsync();
    Assert.Equal(2, users.Count);
  }

  // Note: GetCurrentUser and Logout require HttpContext which can't be easily mocked in unit tests
  // These endpoints are fully tested in integration tests where HttpContext is available

  [Fact]
  public async Task CreateGuestSession_GeneratesValidGuestSessionToken()
  {
    // Act
    var result = await AuthController.CreateGuestSession();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = okResult.Value;
    var guestSessionToken = response?.GetType().GetProperty("guestSessionToken")?.GetValue(response, null) as string;

    Assert.NotNull(guestSessionToken);

    // Token should be uppercase hex string (GUID without dashes)
    Assert.Equal(32, guestSessionToken.Length);
    Assert.Matches("^[A-F0-9]{32}$", guestSessionToken);
  }

  [Fact]
  public async Task CreateGuestSession_SetsCorrectUserProperties()
  {
    // Act
    var result = await AuthController.CreateGuestSession();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = okResult.Value;
    var guestSessionToken = response?.GetType().GetProperty("guestSessionToken")?.GetValue(response, null) as string;

    var savedUser = await Context.Users.FirstOrDefaultAsync(u => u.GuestSessionToken == guestSessionToken);

    Assert.NotNull(savedUser);
    Assert.Null(savedUser.DiscordId);
    Assert.Null(savedUser.GlobalName);
    Assert.Null(savedUser.AvatarHash);
    Assert.False(savedUser.IsAdmin);
    Assert.True(savedUser.CreatedAt <= DateTime.UtcNow);
    Assert.True(savedUser.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
  }

  [Fact]
  public async Task CreateGuestSession_CreatesGuestWithPrintRequests()
  {
    // Arrange
    var result = await AuthController.CreateGuestSession();
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = okResult.Value;
    var userId = (Guid?)response?.GetType().GetProperty("userId")?.GetValue(response, null);

    Assert.NotNull(userId);

    // Create a filament for the request
    var filament = TestDataFactory.CreateTestFilament();
    await Context.Filaments.AddAsync(filament);
    await Context.SaveChangesAsync();

    // Create a print request for the guest user
    var printRequest = TestDataFactory.CreateTestPrintRequest(
        userId: userId.Value,
        filament: filament
    );
    await Context.PrintRequests.AddAsync(printRequest);
    await Context.SaveChangesAsync();

    // Act - verify the guest user has the request
    var user = await Context.Users
        .Include(u => u.PrintRequests)
        .FirstOrDefaultAsync(u => u.Id == userId.Value);

    // Assert
    Assert.NotNull(user);
    Assert.Single(user.PrintRequests);
    Assert.Equal(printRequest.Id, user.PrintRequests.First().Id);
  }

  [Fact]
  public async Task CreateGuestSession_CanCreateMultipleGuestSessionsWithoutCollision()
  {
    // Arrange & Act
    var tasks = Enumerable.Range(0, 10)
        .Select(_ => AuthController.CreateGuestSession())
        .ToList();

    var results = await Task.WhenAll(tasks);

    // Assert
    var tokens = results
        .Select(r => ((OkObjectResult)r).Value)
        .Select(v => v?.GetType().GetProperty("guestSessionToken")?.GetValue(v, null) as string)
        .ToList();

    // All tokens should be unique
    Assert.Equal(10, tokens.Distinct().Count());

    // All tokens should exist in database
    var guestUsers = await Context.Users.Where(u => u.GuestSessionToken != null && u.DiscordId == null).ToListAsync();
    Assert.Equal(10, guestUsers.Count);
  }
}
