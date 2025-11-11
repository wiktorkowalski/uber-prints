using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UberPrints.Server.Data;
using UberPrints.Server.Models;

namespace UberPrints.Server.Services;

public class DiscordService
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly IConfiguration _configuration;
  private readonly ILogger<DiscordService> _logger;
  private readonly HttpClient _httpClient;

  public DiscordService(
      IServiceScopeFactory scopeFactory,
      IConfiguration configuration,
      ILogger<DiscordService> logger,
      HttpClient httpClient)
  {
    _scopeFactory = scopeFactory;
    _configuration = configuration;
    _logger = logger;
    _httpClient = httpClient;

    // Set up Discord bot authorization
    var botToken = _configuration["Discord:BotToken"];
    if (!string.IsNullOrEmpty(botToken))
    {
      _httpClient.DefaultRequestHeaders.Clear();
      _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bot {botToken}");
    }
  }

  public async Task NotifyAdminsNewRequestAsync(PrintRequest request)
  {
    try
    {
      var botToken = _configuration["Discord:BotToken"];
      if (string.IsNullOrEmpty(botToken))
      {
        _logger.LogWarning("Discord bot token not configured, skipping admin notification");
        return;
      }

      // Create scope for database access
      using var scope = _scopeFactory.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // Get all admin users with Discord IDs
      var admins = await context.Users
          .Where(u => u.IsAdmin && u.DiscordId != null)
          .ToListAsync();

      if (admins.Count == 0)
      {
        _logger.LogWarning("No admin users with Discord IDs found");
        return;
      }

      var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5173";
      var requestUrl = $"{frontendUrl}/request/{request.Id}";

      var message = $"**New Print Request**\n\n" +
                   $"From: **{request.RequesterName}**\n" +
                   $"Delivery: {(request.RequestDelivery ? "Yes" : "No")}\n" +
                   $"Description: {(string.IsNullOrEmpty(request.Notes) ? "None" : request.Notes)}\n\n" +
                   $"View: {requestUrl}";

      foreach (var admin in admins)
      {
        await SendDirectMessageAsync(admin.DiscordId!, message);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to notify admins about new request {RequestId}", request.Id);
      // Don't throw - notification failures shouldn't block request creation
    }
  }

  public async Task NotifyRequesterStatusChangeAsync(
      PrintRequest request,
      RequestStatusEnum oldStatus,
      RequestStatusEnum newStatus)
  {
    try
    {
      // Check if user opted in for notifications
      if (!request.NotifyOnStatusChange)
      {
        _logger.LogDebug("User opted out of notifications for request {RequestId}", request.Id);
        return;
      }

      var botToken = _configuration["Discord:BotToken"];
      if (string.IsNullOrEmpty(botToken))
      {
        _logger.LogWarning("Discord bot token not configured, skipping requester notification");
        return;
      }

      // Get the user with Discord ID
      if (request.UserId == null)
      {
        _logger.LogDebug("Request {RequestId} has no associated user, skipping notification", request.Id);
        return;
      }

      // Create scope for database access
      using var scope = _scopeFactory.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      var user = await context.Users.FindAsync(request.UserId);
      if (user?.DiscordId == null)
      {
        _logger.LogDebug("User {UserId} has no Discord ID, skipping notification", request.UserId);
        return;
      }

      var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5173";
      var requestUrl = $"{frontendUrl}/request/{request.Id}";

      var message = $"**Print Request Status Update**\n\n" +
                   $"Your request status changed:\n" +
                   $"**{oldStatus}** → **{newStatus}**\n\n" +
                   $"View: {requestUrl}";

      await SendDirectMessageAsync(user.DiscordId, message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to notify requester about status change for request {RequestId}", request.Id);
      // Don't throw - notification failures shouldn't block status updates
    }
  }

  private async Task SendDirectMessageAsync(string discordUserId, string message)
  {
    try
    {
      // Step 1: Create DM channel with user
      var createDmPayload = new { recipient_id = discordUserId };
      var createDmContent = new StringContent(
          JsonSerializer.Serialize(createDmPayload),
          Encoding.UTF8,
          "application/json");

      var createDmResponse = await _httpClient.PostAsync(
          "https://discord.com/api/v10/users/@me/channels",
          createDmContent);

      if (!createDmResponse.IsSuccessStatusCode)
      {
        var errorContent = await createDmResponse.Content.ReadAsStringAsync();
        _logger.LogWarning(
            "Failed to create DM channel with user {DiscordUserId}: {StatusCode} - {Error}",
            discordUserId,
            createDmResponse.StatusCode,
            errorContent);
        return;
      }

      var dmChannel = await createDmResponse.Content.ReadFromJsonAsync<DiscordChannel>();
      if (dmChannel?.Id == null)
      {
        _logger.LogWarning("Failed to parse DM channel response for user {DiscordUserId}", discordUserId);
        return;
      }

      // Step 2: Send message to DM channel
      var sendMessagePayload = new { content = message };
      var sendMessageContent = new StringContent(
          JsonSerializer.Serialize(sendMessagePayload),
          Encoding.UTF8,
          "application/json");

      var sendMessageResponse = await _httpClient.PostAsync(
          $"https://discord.com/api/v10/channels/{dmChannel.Id}/messages",
          sendMessageContent);

      if (!sendMessageResponse.IsSuccessStatusCode)
      {
        var errorContent = await sendMessageResponse.Content.ReadAsStringAsync();
        _logger.LogWarning(
            "Failed to send DM to user {DiscordUserId}: {StatusCode} - {Error}",
            discordUserId,
            sendMessageResponse.StatusCode,
            errorContent);
        return;
      }

      _logger.LogInformation("Successfully sent Discord DM to user {DiscordUserId}", discordUserId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error sending Discord DM to user {DiscordUserId}", discordUserId);
    }
  }

  private class DiscordChannel
  {
    [JsonPropertyName("id")]
    public string? Id { get; set; }
  }
}
