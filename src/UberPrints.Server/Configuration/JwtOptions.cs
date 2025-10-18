using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Configuration;

/// <summary>
/// Configuration options for JWT token generation and validation
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret key for signing JWT tokens
    /// Must be at least 32 characters for security
    /// </summary>
    [Required(ErrorMessage = "JWT SecretKey is required")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters long")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT token issuer
    /// </summary>
    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = "UberPrints";

    /// <summary>
    /// JWT token audience
    /// </summary>
    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = "UberPrints";

    /// <summary>
    /// JWT token expiry time in hours
    /// </summary>
    [Range(1, 8760, ErrorMessage = "JWT ExpiryHours must be between 1 and 8760 (1 year)")]
    public int ExpiryHours { get; set; } = 168; // 7 days default
}
