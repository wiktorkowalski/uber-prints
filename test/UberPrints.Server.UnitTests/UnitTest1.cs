using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public TestBase()
    {
        // Create a real in-memory database context for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique database name for each test
            .Options;

        Context = new ApplicationDbContext(options);

        // Create controllers with the real context
        RequestsController = new RequestsController(Context);
        AdminController = new AdminController(Context);
        FilamentsController = new FilamentsController(Context);
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
