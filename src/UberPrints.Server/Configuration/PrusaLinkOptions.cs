using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Configuration;

/// <summary>
/// Configuration options for PrusaLink integration
/// </summary>
public class PrusaLinkOptions
{
  public const string SectionName = "PrusaLink";

  /// <summary>
  /// IP address of the printer
  /// </summary>
  [Required(ErrorMessage = "PrusaLink IpAddress is required")]
  [MinLength(1, ErrorMessage = "PrusaLink IpAddress cannot be empty")]
  public string IpAddress { get; set; } = string.Empty;

  /// <summary>
  /// API key for PrusaLink authentication
  /// Get this from printer: Settings → Prusa Connect → API Key
  /// </summary>
  [Required(ErrorMessage = "PrusaLink ApiKey is required")]
  [MinLength(1, ErrorMessage = "PrusaLink ApiKey cannot be empty")]
  public string ApiKey { get; set; } = string.Empty;

  /// <summary>
  /// Polling interval in seconds when printer is idle
  /// </summary>
  [Range(5, 300, ErrorMessage = "PollingIntervalIdle must be between 5 and 300 seconds")]
  public int PollingIntervalIdle { get; set; } = 30;

  /// <summary>
  /// Polling interval in seconds when printer is actively printing
  /// </summary>
  [Range(1, 60, ErrorMessage = "PollingIntervalActive must be between 1 and 60 seconds")]
  public int PollingIntervalActive { get; set; } = 5;

  /// <summary>
  /// Request timeout in seconds for API calls
  /// </summary>
  [Range(5, 120, ErrorMessage = "RequestTimeout must be between 5 and 120 seconds")]
  public int RequestTimeout { get; set; } = 30;

  /// <summary>
  /// Maximum number of retry attempts for failed API calls
  /// </summary>
  [Range(0, 10, ErrorMessage = "MaxRetryAttempts must be between 0 and 10")]
  public int MaxRetryAttempts { get; set; } = 3;
}
