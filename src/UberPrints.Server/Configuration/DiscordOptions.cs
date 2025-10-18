using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Configuration;

/// <summary>
/// Configuration options for Discord OAuth authentication
/// </summary>
public class DiscordOptions
{
    public const string SectionName = "Discord";

    /// <summary>
    /// Discord OAuth Client ID
    /// Get this from https://discord.com/developers/applications
    /// </summary>
    [Required(ErrorMessage = "Discord ClientId is required")]
    [MinLength(1, ErrorMessage = "Discord ClientId cannot be empty")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Discord OAuth Client Secret
    /// Get this from https://discord.com/developers/applications
    /// </summary>
    [Required(ErrorMessage = "Discord ClientSecret is required")]
    [MinLength(1, ErrorMessage = "Discord ClientSecret cannot be empty")]
    public string ClientSecret { get; set; } = string.Empty;
}
