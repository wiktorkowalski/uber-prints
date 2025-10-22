using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        RequestsController = new RequestsController(Context, ChangeTrackingService);
        AdminController = new AdminController(Context, ChangeTrackingService);
        FilamentsController = new FilamentsController(Context);
        AuthController = new AuthController(Context, Configuration);

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
}
