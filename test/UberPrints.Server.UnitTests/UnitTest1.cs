using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using UberPrints.Server.Controllers;
using UberPrints.Server.Data;
using UberPrints.Server.Models;
using UberPrints.Server.DTOs;

namespace UberPrints.Server.UnitTests;

public class TestBase
{
    protected readonly ApplicationDbContext Context;
    protected readonly RequestsController RequestsController;
    protected readonly AdminController AdminController;
    protected readonly FilamentsController FilamentsController;
    protected readonly AuthController AuthController;
    protected readonly IConfiguration Configuration;

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

        // Create controllers with the real context
        RequestsController = new RequestsController(Context);
        AdminController = new AdminController(Context);
        FilamentsController = new FilamentsController(Context);
        AuthController = new AuthController(Context, Configuration);
    }
}

public class UnitTest1 : TestBase
{
    [Fact]
    public void TestInfrastructureSetup()
    {
        // Test that the basic infrastructure is properly set up
        Assert.NotNull(Context);
        Assert.NotNull(RequestsController);
        Assert.NotNull(AdminController);
        Assert.NotNull(FilamentsController);
    }
}
