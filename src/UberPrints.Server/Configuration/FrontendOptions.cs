using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Configuration;

/// <summary>
/// Configuration options for frontend URL and CORS
/// </summary>
public class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>
    /// Frontend application URL for CORS configuration
    /// In development: http://localhost:5173
    /// In production: https://your-domain.com
    /// </summary>
    [Required(ErrorMessage = "Frontend URL is required")]
    [Url(ErrorMessage = "Frontend URL must be a valid URL")]
    public string Url { get; set; } = "http://localhost:5173";
}
