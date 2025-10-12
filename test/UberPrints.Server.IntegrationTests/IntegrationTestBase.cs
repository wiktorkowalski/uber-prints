using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using UberPrints.Server.Data;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class IntegrationTestBase : IClassFixture<IntegrationTestFactory>, IAsyncLifetime
{
  protected readonly HttpClient Client;
  protected readonly IntegrationTestFactory Factory;

  protected IntegrationTestBase(IntegrationTestFactory factory)
  {
    Factory = factory;
    Client = factory.CreateClient();
  }

  public virtual Task InitializeAsync() => Task.CompletedTask;

  public virtual async Task DisposeAsync()
  {
    // Reset database between tests
    await Factory.ResetDatabaseAsync();
  }
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

    builder.UseEnvironment("Testing");
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
