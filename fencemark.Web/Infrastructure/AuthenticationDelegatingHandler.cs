using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;

namespace fencemark.Web.Infrastructure;

/// <summary>
/// Delegating handler that acquires tokens for the API and forwards them with requests.
/// Uses ITokenAcquisition to get tokens specifically scoped for our API.
/// </summary>
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationDelegatingHandler> _logger;

    public AuthenticationDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        ITokenAcquisition tokenAcquisition,
        IConfiguration configuration,
        ILogger<AuthenticationDelegatingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenAcquisition = tokenAcquisition;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        _logger.LogInformation("Auth Handler - Request to: {Uri}", request.RequestUri);
        _logger.LogInformation("Auth Handler - User authenticated: {IsAuth}", httpContext?.User?.Identity?.IsAuthenticated);
        
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                // Get the API scope from configuration
                var scopesConfig = _configuration["AzureAd:Scopes"];
                _logger.LogInformation("Auth Handler - Raw scopes from config: '{ScopesConfig}'", scopesConfig);
                
                // Use the full scope URI matching what's configured in Azure
                var scopes = new[] { "api://5b204301-0113-4b40-bd2e-e0ef8be99f48/access_as_user" };
                
                _logger.LogInformation("Auth Handler - Requesting token for scopes: {Scopes}", string.Join(", ", scopes));
                
                // Use ITokenAcquisition to get a token specifically for our API
                var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    // Decode the JWT to see what type it is
                    var parts = accessToken.Split('.');
                    if (parts.Length >= 2)
                    {
                        try
                        {
                            var payload = System.Text.Encoding.UTF8.GetString(
                                Convert.FromBase64String(parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=')));
                            _logger.LogInformation("Auth Handler - Token payload snippet: {Payload}", 
                                payload.Substring(0, Math.Min(300, payload.Length)));
                        }
                        catch { }
                    }
                    
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    _logger.LogInformation("Auth Handler - Added Bearer token (length: {Length})", accessToken.Length);
                }
                else
                {
                    _logger.LogWarning("Auth Handler - Token acquisition returned empty token");
                }
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                _logger.LogWarning("Auth Handler - User needs to re-authenticate: {Message}", ex.Message);
                // Let the request proceed without auth - will get 401 which may trigger re-auth
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auth Handler - Failed to acquire token: {Message}", ex.Message);
                // Fall back to stored access token if available
                var storedToken = await httpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(storedToken))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", storedToken);
                    _logger.LogInformation("Auth Handler - Falling back to stored token (length: {Length})", storedToken.Length);
                }
            }
        }
        else
        {
            _logger.LogWarning("Auth Handler - User not authenticated");
        }

        var response = await base.SendAsync(request, cancellationToken);
        _logger.LogInformation("Auth Handler - Response status: {StatusCode}", response.StatusCode);
        
        return response;
    }
}
