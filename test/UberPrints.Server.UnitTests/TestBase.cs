using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using UberPrints.Server.Configuration;
using UberPrints.Server.Controllers;
using UberPrints.Server.Data;
using UberPrints.Server.Models;
using UberPrints.Server.Services;

namespace UberPrints.Server.UnitTests;

public class TestBase
{
  protected readonly ApplicationDbContext Context;
  protected readonly IChangeTrackingService ChangeTrackingService;
  protected readonly RequestsController RequestsController;
  protected readonly AdminController AdminController;
  protected readonly FilamentsController FilamentsController;
  protected readonly AuthController AuthController;
  protected readonly IConfiguration Configuration;
  protected User TestAuthenticatedUser;

  public TestBase()
  {
    // Create a real in-memory database context for testing
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique database name for each test
        .Options;

    Context = new ApplicationDbContext(options);

    // Create test configuration
    var configData = new Dictionary<string, string>
        {
            {"Jwt:SecretKey", "ThisIsATestSecretKeyWith32Characters!!"},
            {"Jwt:Issuer", "UberPrintsTest"},
            {"Jwt:Audience", "UberPrintsTest"},
            {"Jwt:ExpiryHours", "1"},
            {"Frontend:Url", "http://localhost:5173"},
            {"Discord:ClientId", "test-client-id"},
            {"Discord:ClientSecret", "test-client-secret"}
        };

    Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(configData!)
        .Build();

    // Create a test authenticated user and add to database
    TestAuthenticatedUser = new User
    {
      Id = Guid.NewGuid(),
      DiscordId = "123456789",
      Username = "TestUser",
      GlobalName = "Test User",
      AvatarHash = "abcd1234"
    };
    Context.Users.Add(TestAuthenticatedUser);
    Context.SaveChanges();

    // Create change tracking service
    ChangeTrackingService = new ChangeTrackingService(Context);

    // Create controllers with the real context
    var discordService = new DiscordService(
        Mock.Of<IServiceScopeFactory>(),
        Configuration,
        NullLogger<DiscordService>.Instance,
        new HttpClient(new BlockOutboundHttpHandler()));
    var thermalPrinterService = new ThermalPrinterService(
        Configuration,
        NullLogger<ThermalPrinterService>.Instance,
        new HttpClient(new BlockOutboundHttpHandler()),
        Options.Create(new ThermalPrinterOptions()));
    RequestsController = new RequestsController(Context, ChangeTrackingService, discordService, thermalPrinterService, Mock.Of<IServiceScopeFactory>());
    AdminController = new AdminController(Context, ChangeTrackingService, discordService, Mock.Of<IServiceScopeFactory>());
    FilamentsController = new FilamentsController(Context);
    var httpClientFactory = new Mock<IHttpClientFactory>();
    httpClientFactory
        .Setup(f => f.CreateClient(It.IsAny<string>()))
        .Returns(() => new HttpClient(new BlockOutboundHttpHandler()));
    AuthController = new AuthController(Context, Configuration, httpClientFactory.Object, NullLogger<AuthController>.Instance);

    // Set up authentication for RequestsController
    SetupControllerContext(RequestsController, TestAuthenticatedUser.Id);
    SetupControllerContext(AdminController, TestAuthenticatedUser.Id);
  }

  protected void SetupControllerContext(ControllerBase controller, Guid userId)
  {
    var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "TestUser")
        };
    var identity = new ClaimsIdentity(claims, "TestAuthType");
    var claimsPrincipal = new ClaimsPrincipal(identity);

    var httpContext = new DefaultHttpContext
    {
      User = claimsPrincipal
    };

    controller.ControllerContext = new ControllerContext
    {
      HttpContext = httpContext
    };
  }

  // ThermalPrinterService and DiscordService target real endpoints; unit tests must never reach them
  private sealed class BlockOutboundHttpHandler : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
      Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
  }
}
