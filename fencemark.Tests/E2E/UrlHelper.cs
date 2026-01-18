namespace fencemark.Tests.E2E;

/// <summary>
/// Helper class for URL normalization and manipulation in E2E tests
/// </summary>
public static class UrlHelper
{
    /// <summary>
    /// Normalizes a URL to ensure it has a protocol (https:// or http://)
    /// </summary>
    /// <param name="url">The URL to normalize</param>
    /// <param name="fallbackUrl">Optional fallback URL if input is null or empty. Defaults to http://localhost:5000</param>
    /// <returns>A normalized URL with protocol</returns>
    public static string NormalizeUrl(string? url, string fallbackUrl = "http://localhost:5000")
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return fallbackUrl;
        }

        var trimmedUrl = url.Trim();
        
        // If URL already has a protocol, return as-is
        if (trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmedUrl;
        }

        // If URL looks like localhost, use http://
        if (trimmedUrl.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return $"http://{trimmedUrl}";
        }

        // For all other URLs (production domains), default to https://
        return $"https://{trimmedUrl}";
    }
}
