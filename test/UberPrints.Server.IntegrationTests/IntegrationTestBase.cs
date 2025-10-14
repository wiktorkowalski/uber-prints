using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class IntegrationTestBase : IClassFixture<IntegrationTestFactory>, IAsyncLifetime
{
  protected readonly HttpClient Client;
  protected readonly IntegrationTestFactory Factory;
  protected string? GuestSessionToken;

  protected IntegrationTestBase(IntegrationTestFactory factory)
  {
    Factory = factory;
    Client = factory.CreateClient();
  }

  public virtual async Task InitializeAsync()
  {
    // Create a guest session for tests
    var response = await Client.PostAsync("/api/auth/guest", null);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<GuestSessionResponse>();
    GuestSessionToken = result?.guestSessionToken;

    // Add the guest session token to default request headers
    if (GuestSessionToken != null)
    {
      Client.DefaultRequestHeaders.Add("X-Guest-Session-Token", GuestSessionToken);
    }
  }

  public virtual async Task DisposeAsync()
  {
    // Reset database between tests
    await Factory.ResetDatabaseAsync();
  }

  private record GuestSessionResponse(string guestSessionToken, string username);
}

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  private readonly PostgreSqlContainer _dbContainer;

  public IntegrationTestFactory()
  {
    _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .WithDatabase("uberprints_test")
        .WithUsername("postgres")
        .WithPassword("postgres_test_pwd")
        .Build();
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureServices(services =>
    {
      // Remove the existing DbContext registration
      var descriptor = services.SingleOrDefault(
              d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

      if (descriptor != null)
      {
        services.Remove(descriptor);
      }

      // Add DbContext using the test container connection string
      services.AddDbContext<ApplicationDbContext>(options =>
          {
            options.UseNpgsql(_dbContainer.GetConnectionString());
          });
    });

    // Disable authentication and authorization for integration tests
    builder.ConfigureServices(services =>
    {
      // Remove existing authorization handlers
      services.RemoveAll<IAuthorizationHandler>();

      // Add a permissive authorization handler that allows everything
      services.AddSingleton<IAuthorizationHandler, AllowAnonymousAuthorizationHandler>();
    });

    builder.UseEnvironment("Testing");
  }

  // Authorization handler that allows all requests (for testing only)
  private class AllowAnonymousAuthorizationHandler : IAuthorizationHandler
  {
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
      foreach (var requirement in context.PendingRequirements.ToList())
      {
        context.Succeed(requirement);
      }
      return Task.CompletedTask;
    }
  }

  public async Task InitializeAsync()
  {
    // Start the PostgreSQL container
    await _dbContainer.StartAsync();

    // Apply migrations
    using var scope = Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
  }

  public new async Task DisposeAsync()
  {
    await _dbContainer.DisposeAsync();
    await base.DisposeAsync();
  }

  public async Task ResetDatabaseAsync()
  {
    using var scope = Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Delete all data from tables
    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"StatusHistories\" CASCADE");
    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"PrintRequests\" CASCADE");
    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Filaments\" CASCADE");
    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Users\" CASCADE");
  }
}
