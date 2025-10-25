using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using UberPrints.Server.Configuration;
using UberPrints.Server.Data;
using UberPrints.Server.Services;

// Load environment variables from .env file (for local development)
// Looks for .env in the project root (2 levels up from bin/Debug/net10.0)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
Env.Load(envPath, new LoadOptions(
    setEnvVars: true,           // Set environment variables
    clobberExistingVars: false, // Don't override existing env vars (they take precedence)
    onlyExactPath: true         // Only load if file exists at exact path
));

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
// This allows accessing env vars through IConfiguration
builder.Configuration.AddEnvironmentVariables();

// Configure strongly-typed configuration with validation
// These will be validated on startup and injected via IOptions<T>
builder.Services.AddOptions<DiscordOptions>()
    .Bind(builder.Configuration.GetSection(DiscordOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<FrontendOptions>()
    .Bind(builder.Configuration.GetSection(FrontendOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CameraOptions>()
    .Bind(builder.Configuration.GetSection(CameraOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Configure forwarded headers for reverse proxy support (Cloudflare Tunnel)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust all proxies (Cloudflare Tunnel)
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add application services
builder.Services.AddScoped<IChangeTrackingService, ChangeTrackingService>();

// Add camera streaming services
builder.Services.AddSingleton<StreamStateService>();
builder.Services.AddSingleton<CameraStreamingService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<CameraStreamingService>());

// Add session support for guest token tracking
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP for local development
});

// Configure Authentication with both Cookie and JWT Bearer support
// Bind configuration to strongly-typed options for use during setup
// Note: Validation happens on startup via ValidateOnStart()
var jwtOptions = new JwtOptions();
builder.Configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);

var discordOptions = new DiscordOptions();
builder.Configuration.GetSection(DiscordOptions.SectionName).Bind(discordOptions);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Discord";
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/api/auth/login";
    options.Cookie.Name = "UberPrints.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);

    // Return 401 instead of redirecting for API requests
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
    };
})
.AddDiscord(options =>
{
    options.ClientId = discordOptions.ClientId;
    options.ClientSecret = discordOptions.ClientSecret;
    options.Scope.Add("identify");
    options.SaveTokens = true;
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

builder.Services.AddAuthorization();

// Configure CORS using FrontendOptions
var frontendOptions = new FrontendOptions();
builder.Configuration.GetSection(FrontendOptions.SectionName).Bind(frontendOptions);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendOptions.Url)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Apply pending database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        logger.LogInformation("Checking for pending database migrations...");
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();

        if (pendingMigrations.Any())
        {
            logger.LogInformation("Found {Count} pending migration(s): {Migrations}",
                pendingMigrations.Count,
                string.Join(", ", pendingMigrations));

            logger.LogInformation("Applying database migrations...");
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date. No pending migrations.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Use forwarded headers (must be before other middleware)
app.UseForwardedHeaders();

app.UseCors();
app.UseSession();

// Configure content type provider for HLS files
var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
contentTypeProvider.Mappings[".ts"] = "video/mp2t";

// Serve static files from wwwroot with custom headers for HLS files
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = ctx =>
    {
        // Set appropriate cache headers for HLS files
        if (ctx.File.PhysicalPath?.Contains("/stream/") == true)
        {
            if (ctx.File.Name.EndsWith(".m3u8"))
            {
                // Playlist should not be cached
                ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            }
            else if (ctx.File.Name.EndsWith(".ts"))
            {
                // Segments can be cached briefly
                ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=1");
            }

            // Add CORS headers for stream files
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        }
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Fallback to index.html for SPA routing (must be after MapControllers)
app.MapFallbackToFile("index.html");

app.Run();
