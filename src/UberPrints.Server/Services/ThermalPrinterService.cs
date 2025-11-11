using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UberPrints.Server.Models;

namespace UberPrints.Server.Services;

public class ThermalPrinterService
{
  private readonly IConfiguration _configuration;
  private readonly ILogger<ThermalPrinterService> _logger;
  private readonly HttpClient _httpClient;
  private const string PRINTER_API_URL = "https://printer.vicio.ovh/api/printer/custom";

  public ThermalPrinterService(
      IConfiguration configuration,
      ILogger<ThermalPrinterService> logger,
      HttpClient httpClient)
  {
    _configuration = configuration;
    _logger = logger;
    _httpClient = httpClient;
  }

  public async Task PrintNewRequestAsync(PrintRequest request)
  {
    try
    {
      var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5173";
      var requestUrl = $"{frontendUrl}/requests/{request.Id}";

      // Extract filename from URL if possible
      var modelFileName = "N/A";
      if (!string.IsNullOrEmpty(request.ModelUrl))
      {
        try
        {
          var uri = new Uri(request.ModelUrl);
          modelFileName = Path.GetFileName(uri.LocalPath);
          if (string.IsNullOrEmpty(modelFileName))
          {
            modelFileName = request.ModelUrl;
          }
        }
        catch
        {
          modelFileName = request.ModelUrl;
        }
      }

      // Truncate long strings for printing
      var truncatedFileName = modelFileName.Length > 30
          ? modelFileName.Substring(0, 27) + "..."
          : modelFileName;

      var truncatedNotes = request.Notes?.Length > 100
          ? request.Notes.Substring(0, 97) + "..."
          : request.Notes ?? "None";

      var printRequest = new ThermalPrintRequest
      {
        Content = new List<ThermalPrintContent>
        {
          // Header
          new ThermalPrintContent
          {
            Type = "Separator",
            SeparatorChar = "=",
            SeparatorLength = 32,
            Alignment = "Center",
            Style = new[] { "DoubleHeight", "DoubleWidth" }
          },
          new ThermalPrintContent
          {
            Type = "Text",
            Content = "NEW PRINT REQUEST",
            Alignment = "Center",
            Style = new[] { "Bold", "DoubleHeight", "DoubleWidth" }
          },
          new ThermalPrintContent
          {
            Type = "Separator",
            SeparatorChar = "=",
            SeparatorLength = 32,
            Alignment = "Center",
            Style = new[] { "DoubleHeight", "DoubleWidth" }
          },
          new ThermalPrintContent
          {
            Type = "LineFeed",
            Lines = 1
          },

          // Order details
          new ThermalPrintContent
          {
            Type = "Text",
            Content = $"Order #: {request.Id.ToString().Substring(0, 8)}",
            Alignment = "Left",
            Style = new[] { "Bold" }
          },
          new ThermalPrintContent
          {
            Type = "Text",
            Content = $"Created: {request.CreatedAt:yyyy-MM-dd HH:mm}",
            Alignment = "Left"
          },
          new ThermalPrintContent
          {
            Type = "LineFeed",
            Lines = 1
          },

          // Requester info
          new ThermalPrintContent
          {
            Type = "Text",
            Content = $"Requester: {request.RequesterName}",
            Alignment = "Left"
          },
          new ThermalPrintContent
          {
            Type = "Text",
            Content = $"Delivery: {(request.RequestDelivery ? "Yes" : "No")}",
            Alignment = "Left"
          },
          new ThermalPrintContent
          {
            Type = "LineFeed",
            Lines = 1
          },

          // Filament info (will be populated if available)
          new ThermalPrintContent
          {
            Type = "Text",
            Content = request.Filament != null
                ? $"Filament: {request.Filament.Brand} {request.Filament.Colour}"
                : "Filament: Not specified",
            Alignment = "Left"
          },
          new ThermalPrintContent
          {
            Type = "LineFeed",
            Lines = 1
          },

          // Model info
          new ThermalPrintContent
          {
            Type = "Text",
            Content = $"Model: {truncatedFileName}",
            Alignment = "Left"
          },
          new ThermalPrintContent
          {
            Type = "LineFeed",
            Lines = 1
          },

          // Description
          new ThermalPrintContent
          {
            Type = "Text",
            Content = "Description:",
            Alignment = "Left",
            Style = new[] { "Bold" }
          },
          new ThermalPrintContent
          {
            Type = "Text",
            Content = truncatedNotes,
            Alignment = "Left"
          },
          new ThermalPrintContent
          {
            Type = "LineFeed",
            Lines = 1
          },

          // Separator before QR
          new ThermalPrintContent
          {
            Type = "Separator",
            SeparatorChar = "-",
            SeparatorLength = 32,
            Alignment = "Center"
          },
          new ThermalPrintContent
          {
            Type = "Text",
            Content = "SCAN TO VIEW DETAILS",
            Alignment = "Center",
            Style = new[] { "Bold" }
          },
          new ThermalPrintContent
          {
            Type = "Separator",
            SeparatorChar = "-",
            SeparatorLength = 32,
            Alignment = "Center"
          },
          new ThermalPrintContent
          {
            Type = "LineFeed",
            Lines = 1
          },

          // QR Code - ExtraLarge size
          new ThermalPrintContent
          {
            Type = "QRCode",
            Content = requestUrl,
            Alignment = "Center",
            QrCodeOptions = new QRCodeOptions
            {
              Model = "Model2",
              Size = "ExtraLarge",
              CorrectionLevel = "Percent15"
            }
          },
          new ThermalPrintContent
          {
            Type = "LineFeed",
            Lines = 1
          },

          // Tracking token
          new ThermalPrintContent
          {
            Type = "Text",
            Content = $"Track: {request.GuestTrackingToken?.Substring(0, 8) ?? "N/A"}",
            Alignment = "Center"
          },

          // Footer separator
          new ThermalPrintContent
          {
            Type = "Separator",
            SeparatorChar = "=",
            SeparatorLength = 32,
            Alignment = "Center"
          },

          // Cut paper
          new ThermalPrintContent
          {
            Type = "Cut",
            PartialCut = false
          }
        },
        Source = "UberPrints",
        Options = new PrintOptions
        {
          AutoCut = true,
          FeedLinesAfterPrint = 3
        }
      };

      var jsonContent = JsonSerializer.Serialize(printRequest, new JsonSerializerOptions
      {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
      });

      var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

      var response = await _httpClient.PostAsync(PRINTER_API_URL, content);

      if (response.IsSuccessStatusCode)
      {
        _logger.LogInformation("Successfully printed request {RequestId} to thermal printer", request.Id);
      }
      else
      {
        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogWarning(
            "Failed to print request {RequestId} to thermal printer: {StatusCode} - {Error}",
            request.Id,
            response.StatusCode,
            errorContent);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error printing request {RequestId} to thermal printer", request.Id);
      // Don't throw - printer failures shouldn't block request creation
    }
  }

  // DTOs for thermal printer API
  private class ThermalPrintRequest
  {
    [JsonPropertyName("content")]
    public List<ThermalPrintContent> Content { get; set; } = new();

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("options")]
    public PrintOptions? Options { get; set; }
  }

  private class ThermalPrintContent
  {
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("alignment")]
    public string? Alignment { get; set; }

    [JsonPropertyName("style")]
    public string[]? Style { get; set; }

    [JsonPropertyName("qrCodeOptions")]
    public QRCodeOptions? QrCodeOptions { get; set; }

    [JsonPropertyName("lines")]
    public int? Lines { get; set; }

    [JsonPropertyName("partialCut")]
    public bool? PartialCut { get; set; }

    [JsonPropertyName("separatorChar")]
    public string? SeparatorChar { get; set; }

    [JsonPropertyName("separatorLength")]
    public int? SeparatorLength { get; set; }
  }

  private class QRCodeOptions
  {
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("correctionLevel")]
    public string? CorrectionLevel { get; set; }
  }

  private class PrintOptions
  {
    [JsonPropertyName("autoCut")]
    public bool AutoCut { get; set; } = true;

    [JsonPropertyName("feedLinesAfterPrint")]
    public int FeedLinesAfterPrint { get; set; } = 3;
  }
}
