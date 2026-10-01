using System.Text.Json;
using Microsoft.Extensions.Options;
using UberPrints.Server.Configuration;

namespace UberPrints.Server.Services;

/// <summary>
/// Client for interacting with PrusaLink API
/// Based on PrusaLink API v1: https://github.com/prusa3d/Prusa-Link-Web/blob/master/spec/openapi.yaml
/// </summary>
public class PrusaLinkClient
{
  private readonly HttpClient _httpClient;
  private readonly PrusaLinkOptions _options;
  private readonly ILogger<PrusaLinkClient> _logger;
  private readonly JsonSerializerOptions _jsonOptions;

  public PrusaLinkClient(
    HttpClient httpClient,
    IOptions<PrusaLinkOptions> options,
    ILogger<PrusaLinkClient> logger)
  {
    _httpClient = httpClient;
    _options = options.Value;
    _logger = logger;
    _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeout);

    _jsonOptions = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };
  }

  /// <summary>
  /// Configure the client for a specific printer
  /// </summary>
  public void ConfigureForPrinter(string ipAddress, string apiKey)
  {
    _httpClient.BaseAddress = new Uri($"http://{ipAddress}");
    _httpClient.DefaultRequestHeaders.Clear();
    _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
  }

  /// <summary>
  /// Get current printer status
  /// </summary>
  public async Task<PrusaLinkStatusResponse?> GetStatusAsync(CancellationToken ct = default)
  {
    try
    {
      var response = await _httpClient.GetAsync("/api/v1/status", ct);
      response.EnsureSuccessStatusCode();

      var json = await response.Content.ReadAsStringAsync(ct);
      return JsonSerializer.Deserialize<PrusaLinkStatusResponse>(json, _jsonOptions);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get printer status");
      return null;
    }
  }

  /// <summary>
  /// Get current job information
  /// </summary>
  public async Task<PrusaLinkJobResponse?> GetJobAsync(CancellationToken ct = default)
  {
    try
    {
      var response = await _httpClient.GetAsync("/api/v1/job", ct);
      response.EnsureSuccessStatusCode();

      var json = await response.Content.ReadAsStringAsync(ct);
      return JsonSerializer.Deserialize<PrusaLinkJobResponse>(json, _jsonOptions);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get job information");
      return null;
    }
  }
}
