using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace UberPrints.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThumbnailController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ThumbnailController> _logger;

    public ThumbnailController(IHttpClientFactory httpClientFactory, ILogger<ThumbnailController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the Open Graph image URL for a given model URL
    /// </summary>
    /// <param name="modelUrl">The URL of the 3D model page</param>
    /// <returns>The thumbnail image URL or null if not found</returns>
    [HttpGet]
    public async Task<ActionResult<string>> GetThumbnail([FromQuery] string modelUrl)
    {
        if (string.IsNullOrWhiteSpace(modelUrl))
        {
            return BadRequest("Model URL is required");
        }

        // Validate URL
        if (!Uri.TryCreate(modelUrl, UriKind.Absolute, out var uri))
        {
            return BadRequest("Invalid URL");
        }

        try
        {
            var hostname = uri.Host.ToLowerInvariant();
            string? thumbnailUrl = null;

            // Platform-specific fetching
            if (hostname.Contains("printables.com"))
            {
                thumbnailUrl = await FetchPrintablesThumbnail(modelUrl);
            }
            else if (hostname.Contains("thingiverse.com"))
            {
                thumbnailUrl = await FetchThingiverseThumbnail(modelUrl);
            }
            else if (hostname.Contains("makerworld.com"))
            {
                thumbnailUrl = await FetchMakerWorldThumbnail(modelUrl);
            }
            else
            {
                // Generic fallback for other sites
                thumbnailUrl = await FetchGenericThumbnail(modelUrl);
            }

            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                _logger.LogInformation("Found thumbnail for {Platform}: {ThumbnailUrl}", hostname, thumbnailUrl);
                return Ok(new { thumbnailUrl });
            }

            _logger.LogInformation("No thumbnail found for: {ModelUrl}", modelUrl);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching thumbnail for: {ModelUrl}", modelUrl);
            return StatusCode(500, "Failed to fetch thumbnail");
        }
    }

    private async Task<string?> FetchPrintablesThumbnail(string modelUrl)
    {
        _logger.LogInformation("Fetching Printables thumbnail for: {ModelUrl}", modelUrl);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        client.Timeout = TimeSpan.FromSeconds(15);

        var response = await client.GetAsync(modelUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Printables fetch failed: {StatusCode}", response.StatusCode);
            return null;
        }

        var html = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Printables HTML length: {Length}", html.Length);

        // Log a snippet of HTML to see if og:image is present
        var ogImageIndex = html.IndexOf("og:image", StringComparison.OrdinalIgnoreCase);
        if (ogImageIndex > 0)
        {
            var snippet = html.Substring(Math.Max(0, ogImageIndex - 50), Math.Min(200, html.Length - Math.Max(0, ogImageIndex - 50)));
            _logger.LogInformation("Found 'og:image' in HTML: ...{Snippet}...", snippet);
        }
        else
        {
            _logger.LogWarning("No 'og:image' found in Printables HTML");
        }

        var thumbnailUrl = ExtractOpenGraphImage(html);

        if (thumbnailUrl != null)
        {
            _logger.LogInformation("Extracted Printables thumbnail: {ThumbnailUrl}", thumbnailUrl);
        }
        else
        {
            _logger.LogWarning("Failed to extract thumbnail from Printables HTML");
        }

        return thumbnailUrl;
    }

    private async Task<string?> FetchThingiverseThumbnail(string modelUrl)
    {
        _logger.LogInformation("Fetching Thingiverse thumbnail for: {ModelUrl}", modelUrl);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.Timeout = TimeSpan.FromSeconds(10);

        var response = await client.GetAsync(modelUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Thingiverse fetch failed: {StatusCode}", response.StatusCode);
            return null;
        }

        var html = await response.Content.ReadAsStringAsync();
        return ExtractOpenGraphImage(html);
    }

    private async Task<string?> FetchMakerWorldThumbnail(string modelUrl)
    {
        _logger.LogInformation("Fetching MakerWorld thumbnail for: {ModelUrl}", modelUrl);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.Timeout = TimeSpan.FromSeconds(10);

        var response = await client.GetAsync(modelUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MakerWorld fetch failed: {StatusCode}", response.StatusCode);
            return null;
        }

        var html = await response.Content.ReadAsStringAsync();
        return ExtractOpenGraphImage(html);
    }

    private async Task<string?> FetchGenericThumbnail(string modelUrl)
    {
        _logger.LogInformation("Fetching generic thumbnail for: {ModelUrl}", modelUrl);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.Timeout = TimeSpan.FromSeconds(10);

        var response = await client.GetAsync(modelUrl);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var html = await response.Content.ReadAsStringAsync();
        return ExtractOpenGraphImage(html);
    }

    private static string? ExtractOpenGraphImage(string html)
    {
        // Try multiple patterns for og:image
        // Note: Printables uses name="og:image" instead of property="og:image"
        var patterns = new[]
        {
            // Standard property="og:image"
            @"<meta\s+property\s*=\s*[""']og:image[""']\s+content\s*=\s*[""']([^""']+)[""']",
            @"<meta\s+content\s*=\s*[""']([^""']+)[""']\s+property\s*=\s*[""']og:image[""']",
            // Printables uses name="og:image" (non-standard)
            @"<meta\s+name\s*=\s*[""']og:image[""']\s+content\s*=\s*[""']([^""']+)[""']",
            @"<meta\s+content\s*=\s*[""']([^""']+)[""']\s+name\s*=\s*[""']og:image[""']",
            // Without quotes
            @"<meta\s+property\s*=\s*""og:image""\s+content\s*=\s*""([^""]+)""",
            @"<meta\s+name\s*=\s*""og:image""\s+content\s*=\s*""([^""]+)""",
            // Twitter image fallback
            @"<meta\s+name\s*=\s*[""']twitter:image[""']\s+content\s*=\s*[""']([^""']+)[""']",
            @"<meta\s+content\s*=\s*[""']([^""']+)[""']\s+name\s*=\s*[""']twitter:image[""']",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var imageUrl = match.Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    return imageUrl;
                }
            }
        }

        return null;
    }
}
