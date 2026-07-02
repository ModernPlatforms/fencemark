using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace fencemark.ApiService.Services;

/// <summary>
/// Service for acquiring Azure Maps access tokens using Azure AD authentication.
/// Tokens are acquired server-side using managed identity (production) or DefaultAzureCredential (development).
/// </summary>
public interface IAzureMapsTokenService
{
    /// <summary>
    /// Gets an access token for Azure Maps API.
    /// </summary>
    Task<AzureMapsTokenResult> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Azure Maps client ID for map initialization.
    /// </summary>
    string? GetClientId();
}

/// <summary>
/// Result from Azure Maps token acquisition
/// </summary>
public record AzureMapsTokenResult(
    string Token,
    DateTimeOffset ExpiresOn,
    string ClientId,
    bool UseSubscriptionKey = false
);

/// <summary>
/// Configuration options for Azure Maps
/// </summary>
public class AzureMapsOptions
{
    public const string SectionName = "AzureMaps";

    /// <summary>
    /// The Azure Maps account client ID (found in Azure Portal under Azure Maps account -> Authentication)
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The Azure Maps subscription key (for local development fallback only - not used in production)
    /// </summary>
    public string? SubscriptionKey { get; set; }
}

/// <summary>
/// Implementation that acquires Azure Maps tokens via Azure AD using DefaultAzureCredential
/// </summary>
public class AzureMapsTokenService : IAzureMapsTokenService
{
    private readonly AzureMapsOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AzureMapsTokenService> _logger;
    private readonly TokenCredential _credential;

    private const string CacheKey = "AzureMapsToken";
    private const string AzureMapsScope = "https://atlas.microsoft.com/.default";

    public AzureMapsTokenService(
        IOptions<AzureMapsOptions> options,
        IMemoryCache cache,
        ILogger<AzureMapsTokenService> logger)
    {
        _options = options.Value;
        _cache = cache;
        _logger = logger;

        // Use DefaultAzureCredential which works with:
        // - Managed Identity (in Azure)
        // - Azure CLI credentials (local development)
        // - Environment variables
        // - Visual Studio credentials
        _credential = new DefaultAzureCredential();
    }

    public async Task<AzureMapsTokenResult> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cache.TryGetValue(CacheKey, out AzureMapsTokenResult? cachedToken) && cachedToken != null)
        {
            // Return cached token if it's still valid (with 5 minute buffer)
            if (cachedToken.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                _logger.LogDebug("Returning cached Azure Maps token");
                return cachedToken;
            }
        }

        // If subscription key is configured, use it (for local development)
        if (!string.IsNullOrEmpty(_options.SubscriptionKey))
        {
            _logger.LogInformation("Using subscription key for Azure Maps (local development mode)");
            var subKeyResult = new AzureMapsTokenResult(
                Token: _options.SubscriptionKey,
                ExpiresOn: DateTimeOffset.UtcNow.AddHours(24), // Subscription keys don't expire
                ClientId: _options.ClientId ?? "",
                UseSubscriptionKey: true
            );

            // Cache for 1 hour
            _cache.Set(CacheKey, subKeyResult, DateTimeOffset.UtcNow.AddHours(1));
            return subKeyResult;
        }

        // Validate configuration
        if (string.IsNullOrEmpty(_options.ClientId))
        {
            throw new InvalidOperationException(
                "Azure Maps ClientId is not configured. Add AzureMaps:ClientId to appsettings.json");
        }

        try
        {
            _logger.LogInformation("Acquiring new Azure Maps token via DefaultAzureCredential");

            var tokenRequest = new TokenRequestContext(new[] { AzureMapsScope });
            var accessToken = await _credential.GetTokenAsync(tokenRequest, cancellationToken);

            var result = new AzureMapsTokenResult(
                Token: accessToken.Token,
                ExpiresOn: accessToken.ExpiresOn,
                ClientId: _options.ClientId
            );

            // Cache the token (expires 5 minutes before actual expiry)
            var cacheExpiry = accessToken.ExpiresOn.AddMinutes(-5);
            _cache.Set(CacheKey, result, cacheExpiry);

            _logger.LogInformation("Azure Maps token acquired, expires at {ExpiresOn}", accessToken.ExpiresOn);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire Azure Maps token");
            throw;
        }
    }

    public string? GetClientId() => _options.ClientId;
}
