using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using AspNet.Security.OAuth.Discord;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using System.Text.Json;
using Testcontainers.PostgreSql;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;
using UberPrints.Server.Models;
using UberPrints.Server.Services;
using Xunit;

namespace UberPrints.Server.IntegrationTests;

public class IntegrationTestBase : IClassFixture<IntegrationTestFactory>, IAsyncLifetime
{
  // Same serializer settings the server uses, so enums and naming match the real client
  protected JsonSerializerOptions JsonOptions =>
    Factory.Services.GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value.JsonSerializerOptions;

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

    var result = await response.Content.ReadFromJsonAsync<GuestSessionResponse>(JsonOptions);
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
  private const int MaxResetAttempts = 3;

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
    // .invalid never resolves, so even without the HTTP stub no ticket could reach the real printer
    Environment.SetEnvironmentVariable("ThermalPrinter__ApiUrl", "https://printer.invalid/api/Printer");
    Environment.SetEnvironmentVariable("PrusaLink__IpAddress", "127.0.0.1");
    Environment.SetEnvironmentVariable("PrusaLink__ApiKey", "test-api-key");
    Environment.SetEnvironmentVariable("Camera__RtspUrl", "rtsp://127.0.0.1/test");

    _dbContainer = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("uberprints_test")
        .WithUsername("postgres")
        .WithPassword("postgres_test_pwd")
        .Build();
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureServices(services =>
    {
      // The monitoring worker would write printer rows into the shared test DB on a timer
      var monitoringWorkers = services
        .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(PrinterMonitoringService))
        .ToList();
      if (monitoringWorkers.Count != 1)
        throw new InvalidOperationException($"Expected 1 PrinterMonitoringService registration, found {monitoringWorkers.Count}");
      services.Remove(monitoringWorkers[0]);

      // ThermalPrinterService targets the real printer URL and DiscordService the real bot; tests must never reach them
      services.ConfigureHttpClientDefaults(client =>
        client.ConfigurePrimaryHttpMessageHandler(() => new BlockOutboundHttpHandler()));

      // The OAuth handler builds its backchannel outside IHttpClientFactory; Configure runs before its PostConfigure creates it
      services.Configure<DiscordAuthenticationOptions>(DiscordAuthenticationDefaults.AuthenticationScheme, options =>
        options.BackchannelHttpHandler = new BlockOutboundHttpHandler());

      // AddDbContext also registers an options configuration that would still apply Program's UseNpgsql
      services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
      services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

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

  internal sealed class BlockOutboundHttpHandler : HttpMessageHandler
  {
    public static ConcurrentQueue<(Uri Uri, string Body)> BlockedRequests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
      BlockedRequests.Enqueue((request.RequestUri!, body));
      return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
    }
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
    // Stop the host first so nothing queries the database after the container is gone
    await base.DisposeAsync();
    await _dbContainer.DisposeAsync();
  }

  public async Task ResetDatabaseAsync()
  {
    using var scope = Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // NotificationWorker may still be reading a request a test just created, so TRUNCATE may deadlock with it
    for (var attempt = 1; ; attempt++)
    {
      try
      {
        await dbContext.Database.ExecuteSqlRawAsync(
          "TRUNCATE TABLE \"StatusHistories\", \"PrintRequests\", \"Filaments\", \"Users\" CASCADE");
        return;
      }
      catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DeadlockDetected && attempt < MaxResetAttempts)
      {
        await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt));
      }
    }
  }
}
