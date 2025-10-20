using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Testcontainers.PostgreSql;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;
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

  /// <summary>
  /// Creates an authenticated user in the database and returns a JWT token for API requests
  /// </summary>
  protected async Task<string> CreateAuthenticatedUserAndGetToken(
      string discordId = "123456789",
      string username = "testuser",
      string? globalName = "Test User",
      bool isAdmin = false)
  {
    using var scope = Factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var user = new User
    {
      DiscordId = discordId,
      Username = username,
      GlobalName = globalName,
      AvatarHash = "abcdef123456",
      IsAdmin = isAdmin,
      CreatedAt = DateTime.UtcNow
    };

    context.Users.Add(user);
    await context.SaveChangesAsync();

    // Generate JWT token for this user
    var token = GenerateJwtToken(user.Id, username, isAdmin);
    return token;
  }

  /// <summary>
  /// Creates an authenticated user and returns both the user entity and JWT token
  /// </summary>
  protected async Task<(User user, string token)> CreateAuthenticatedUserWithToken(
      string discordId = "123456789",
      string username = "testuser",
      string? globalName = "Test User",
      string? avatarHash = "abcdef123456",
      bool isAdmin = false)
  {
    using var scope = Factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var user = new User
    {
      DiscordId = discordId,
      Username = username,
      GlobalName = globalName,
      AvatarHash = avatarHash,
      IsAdmin = isAdmin,
      CreatedAt = DateTime.UtcNow
    };

    context.Users.Add(user);
    await context.SaveChangesAsync();

    var token = GenerateJwtToken(user.Id, username, isAdmin);
    return (user, token);
  }

  /// <summary>
  /// Creates an HTTP client with authentication header set
  /// </summary>
  protected HttpClient CreateAuthenticatedClient(string token)
  {
    var client = Factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
  }

  /// <summary>
  /// Generates a JWT token for testing (mimics AuthController.GenerateJwtToken)
  /// </summary>
  private string GenerateJwtToken(Guid userId, string username, bool isAdmin)
  {
    // Use a test secret key (must be at least 32 characters)
    var secretKey = "TestSecretKeyForIntegrationTests1234567890";
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
      new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
      new Claim(ClaimTypes.Name, username),
      new Claim("IsAdmin", isAdmin.ToString())
    };

    if (isAdmin)
    {
      claims.Add(new Claim(ClaimTypes.Role, "Admin"));
    }

    var token = new JwtSecurityToken(
        issuer: "UberPrints",
        audience: "UberPrints",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private record GuestSessionResponse(string guestSessionToken, string username);
}

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  private readonly PostgreSqlContainer _dbContainer;

  public IntegrationTestFactory()
  {
    // Set test configuration as environment variables BEFORE Program.cs runs
    // ASP.NET Core maps Jwt__SecretKey (double underscore) to Jwt:SecretKey (colon) in configuration
    // The .env file loads with clobberExistingVars: false, so these existing vars take precedence
    Environment.SetEnvironmentVariable("Jwt__SecretKey", "TestSecretKeyForIntegrationTests1234567890");
    Environment.SetEnvironmentVariable("Jwt__Issuer", "UberPrints");
    Environment.SetEnvironmentVariable("Jwt__Audience", "UberPrints");
    Environment.SetEnvironmentVariable("Jwt__ExpiryHours", "1");
    Environment.SetEnvironmentVariable("Discord__ClientId", "test-client-id");
    Environment.SetEnvironmentVariable("Discord__ClientSecret", "test-client-secret");
    Environment.SetEnvironmentVariable("Frontend__Url", "http://localhost:5173");

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

    // Configure authentication and authorization for tests
    builder.ConfigureServices(services =>
    {
      // Remove existing authorization handlers
      services.RemoveAll<IAuthorizationHandler>();

      // Add a permissive authorization handler that allows everything
      // This bypasses [Authorize] attributes but still validates JWT tokens
      services.AddSingleton<IAuthorizationHandler, AllowAnonymousAuthorizationHandler>();

      // Reconfigure JWT Bearer to use test secret key
      services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
        Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
          // Override the token validation parameters to use test secret
          options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
          {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "UberPrints",
            ValidAudience = "UberPrints",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
              System.Text.Encoding.UTF8.GetBytes("TestSecretKeyForIntegrationTests1234567890"))
          };

          // Disable HTTPS requirement for tests
          options.RequireHttpsMetadata = false;
        });
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
