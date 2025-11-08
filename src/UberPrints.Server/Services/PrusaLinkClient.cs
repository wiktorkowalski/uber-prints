using System.Net.Http.Headers;
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

  /// <summary>
  /// Get printer version/info
  /// </summary>
  public async Task<PrusaLinkVersionResponse?> GetVersionAsync(CancellationToken ct = default)
  {
    try
    {
      var response = await _httpClient.GetAsync("/api/version", ct);
      response.EnsureSuccessStatusCode();

      var json = await response.Content.ReadAsStringAsync(ct);
      return JsonSerializer.Deserialize<PrusaLinkVersionResponse>(json, _jsonOptions);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get printer version");
      return null;
    }
  }

  /// <summary>
  /// Upload a G-code file to the printer
  /// </summary>
  public async Task<bool> UploadFileAsync(
    Stream fileStream,
    string fileName,
    string storage = "local",
    bool printAfterUpload = false,
    bool overwrite = true,
    CancellationToken ct = default)
  {
    try
    {
      using var content = new MultipartFormDataContent();
      var fileContent = new StreamContent(fileStream);
      fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
      content.Add(fileContent, "file", fileName);

      var uri = $"/api/v1/files/{storage}";
      var request = new HttpRequestMessage(HttpMethod.Put, uri)
      {
        Content = content
      };

      if (printAfterUpload)
      {
        request.Headers.Add("Print-After-Upload", "?1");
      }

      if (overwrite)
      {
        request.Headers.Add("Overwrite", "?1");
      }

      var response = await _httpClient.SendAsync(request, ct);
      response.EnsureSuccessStatusCode();

      _logger.LogInformation("Successfully uploaded file: {FileName}", fileName);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
      return false;
    }
  }

  /// <summary>
  /// Start printing a file
  /// </summary>
  public async Task<bool> StartPrintAsync(string filePath, string storage = "local", CancellationToken ct = default)
  {
    try
    {
      var uri = $"/api/v1/files/{storage}/{filePath}";
      var response = await _httpClient.PostAsync(uri, null, ct);

      if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
      {
        _logger.LogWarning("Printer not in correct state to start print. LCD may not be on main screen.");
        return false;
      }

      response.EnsureSuccessStatusCode();
      _logger.LogInformation("Successfully started print: {FilePath}", filePath);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to start print: {FilePath}", filePath);
      return false;
    }
  }

  /// <summary>
  /// Pause the current print job
  /// </summary>
  public async Task<bool> PausePrintAsync(CancellationToken ct = default)
  {
    try
    {
      var content = new StringContent(
        JsonSerializer.Serialize(new { command = "pause" }),
        System.Text.Encoding.UTF8,
        "application/json");

      var response = await _httpClient.PostAsync("/api/v1/job", content, ct);
      response.EnsureSuccessStatusCode();

      _logger.LogInformation("Successfully paused print");
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to pause print");
      return false;
    }
  }

  /// <summary>
  /// Resume the current print job
  /// </summary>
  public async Task<bool> ResumePrintAsync(CancellationToken ct = default)
  {
    try
    {
      var content = new StringContent(
        JsonSerializer.Serialize(new { command = "resume" }),
        System.Text.Encoding.UTF8,
        "application/json");

      var response = await _httpClient.PostAsync("/api/v1/job", content, ct);
      response.EnsureSuccessStatusCode();

      _logger.LogInformation("Successfully resumed print");
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to resume print");
      return false;
    }
  }

  /// <summary>
  /// Cancel the current print job
  /// </summary>
  public async Task<bool> CancelPrintAsync(CancellationToken ct = default)
  {
    try
    {
      var response = await _httpClient.DeleteAsync("/api/v1/job", ct);
      response.EnsureSuccessStatusCode();

      _logger.LogInformation("Successfully cancelled print");
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to cancel print");
      return false;
    }
  }

  /// <summary>
  /// Get a snapshot from the printer's camera
  /// </summary>
  public async Task<byte[]?> GetSnapshotAsync(CancellationToken ct = default)
  {
    try
    {
      // Try common camera snapshot endpoints
      var endpoints = new[] { "/api/v1/cameras/0/snapshot", "/snapshot" };

      foreach (var endpoint in endpoints)
      {
        try
        {
          var response = await _httpClient.GetAsync(endpoint, ct);
          if (response.IsSuccessStatusCode)
          {
            return await response.Content.ReadAsByteArrayAsync(ct);
          }
        }
        catch
        {
          // Try next endpoint
        }
      }

      _logger.LogWarning("No camera snapshot available from any endpoint");
      return null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get camera snapshot");
      return null;
    }
  }

  /// <summary>
  /// Test connection to the printer
  /// </summary>
  public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
  {
    try
    {
      var version = await GetVersionAsync(ct);
      return version != null;
    }
    catch
    {
      return false;
    }
  }
}
