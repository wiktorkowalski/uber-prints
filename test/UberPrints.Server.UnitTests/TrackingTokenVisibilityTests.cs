using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;

namespace UberPrints.Server.UnitTests;

public sealed class TrackingTokenVisibilityTests : TestBase
{
  private const string Token = "ABCDEF0123456789";

  private async Task<(User Owner, PrintRequest Request)> SeedPublicRequestAsync()
  {
    var owner = TestDataFactory.CreateTestUser(username: "Owner");
    var filament = TestDataFactory.CreateTestFilament();
    var request = TestDataFactory.CreateTestPrintRequest(
        userId: owner.Id,
        guestTrackingToken: Token,
        filament: filament,
        isPublic: true);

    await Context.Users.AddAsync(owner);
    await Context.Filaments.AddAsync(filament);
    await Context.PrintRequests.AddAsync(request);
    await Context.SaveChangesAsync();

    return (owner, request);
  }

  private void SetCaller(ClaimsPrincipal principal, string? guestSessionToken = null)
  {
    var httpContext = new DefaultHttpContext { User = principal };
    if (guestSessionToken != null)
    {
      httpContext.Request.Headers["X-Guest-Session-Token"] = guestSessionToken;
    }

    RequestsController.ControllerContext = new ControllerContext { HttpContext = httpContext };
  }

  private static ClaimsPrincipal AuthenticatedUser(Guid userId, bool isAdmin = false)
  {
    List<Claim> claims = [new(ClaimTypes.NameIdentifier, userId.ToString())];
    if (isAdmin)
    {
      claims.Add(new Claim(ClaimTypes.Role, "Admin"));
    }

    return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
  }

  private async Task<PrintRequestDto> GetSingleFromListAsync()
  {
    var result = await RequestsController.GetRequests();
    var okResult = Assert.IsType<OkObjectResult>(result);
    var requests = Assert.IsType<List<PrintRequestDto>>(okResult.Value);
    return Assert.Single(requests);
  }

  private async Task<PrintRequestDto> GetDetailAsync(Guid id)
  {
    var result = await RequestsController.GetRequest(id);
    var okResult = Assert.IsType<OkObjectResult>(result);
    return Assert.IsType<PrintRequestDto>(okResult.Value);
  }

  [Fact]
  public async Task Anonymous_DoesNotSeeToken_OnListAndDetail()
  {
    var (_, request) = await SeedPublicRequestAsync();
    SetCaller(new ClaimsPrincipal(new ClaimsIdentity()));

    Assert.Null((await GetSingleFromListAsync()).GuestTrackingToken);
    Assert.Null((await GetDetailAsync(request.Id)).GuestTrackingToken);
  }

  [Fact]
  public async Task OtherUser_DoesNotSeeToken_OnListAndDetail()
  {
    var (_, request) = await SeedPublicRequestAsync();
    SetCaller(AuthenticatedUser(TestAuthenticatedUser.Id));

    Assert.Null((await GetSingleFromListAsync()).GuestTrackingToken);
    Assert.Null((await GetDetailAsync(request.Id)).GuestTrackingToken);
  }

  [Fact]
  public async Task Owner_SeesToken_OnListAndDetail()
  {
    var (owner, request) = await SeedPublicRequestAsync();
    SetCaller(AuthenticatedUser(owner.Id));

    Assert.Equal(Token, (await GetSingleFromListAsync()).GuestTrackingToken);
    Assert.Equal(Token, (await GetDetailAsync(request.Id)).GuestTrackingToken);
  }

  [Fact]
  public async Task GuestSessionOwner_SeesToken_OnListAndDetail()
  {
    var (owner, request) = await SeedPublicRequestAsync();
    owner.GuestSessionToken = "guest-session-token";
    await Context.SaveChangesAsync();
    SetCaller(new ClaimsPrincipal(new ClaimsIdentity()), guestSessionToken: "guest-session-token");

    Assert.Equal(Token, (await GetSingleFromListAsync()).GuestTrackingToken);
    Assert.Equal(Token, (await GetDetailAsync(request.Id)).GuestTrackingToken);
  }

  [Fact]
  public async Task Admin_SeesToken_OnListAndDetail()
  {
    var (_, request) = await SeedPublicRequestAsync();
    SetCaller(AuthenticatedUser(TestAuthenticatedUser.Id, isAdmin: true));

    Assert.Equal(Token, (await GetSingleFromListAsync()).GuestTrackingToken);
    Assert.Equal(Token, (await GetDetailAsync(request.Id)).GuestTrackingToken);
  }

  [Fact]
  public async Task Anonymous_SeesToken_WhenTrackingByToken()
  {
    await SeedPublicRequestAsync();
    SetCaller(new ClaimsPrincipal(new ClaimsIdentity()));

    var result = await RequestsController.TrackRequest(Token);

    var okResult = Assert.IsType<OkObjectResult>(result);
    var dto = Assert.IsType<PrintRequestDto>(okResult.Value);
    Assert.Equal(Token, dto.GuestTrackingToken);
  }

  [Fact]
  public async Task Creator_SeesToken_InCreateResponse()
  {
    var filament = TestDataFactory.CreateTestFilament(stockAmount: 1000);
    await Context.Filaments.AddAsync(filament);
    await Context.SaveChangesAsync();

    var result = await RequestsController.CreateRequest(
        TestDataFactory.CreatePrintRequestDto(filamentId: filament.Id));

    var createdResult = Assert.IsType<CreatedAtActionResult>(result);
    var dto = Assert.IsType<PrintRequestDto>(createdResult.Value);
    Assert.False(string.IsNullOrEmpty(dto.GuestTrackingToken));
    Assert.Equal(16, dto.GuestTrackingToken.Length);
  }
}
