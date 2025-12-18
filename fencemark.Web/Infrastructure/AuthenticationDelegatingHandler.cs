namespace fencemark.Web.Infrastructure;

/// <summary>
/// Delegating handler that forwards authentication from Blazor Server to API calls.
/// For Blazor Server with cookie auth, we forward the authentication cookie to the API service.
/// This works because both Web and API share the same cookie authentication configuration.
/// </summary>
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // Forward only authentication cookies to the API service
            // Only include the .AspNetCore.Identity.Application cookie (and antiforgery if needed)
            var authCookieName = ".AspNetCore.Identity.Application";
            var antiForgeryPrefix = ".AspNetCore.Antiforgery.";
            
            var authCookies = httpContext.Request.Cookies
                .Where(c => c.Key == authCookieName || c.Key.StartsWith(antiForgeryPrefix))
                .ToList();
            
            if (authCookies.Any())
            {
                var cookieHeader = string.Join("; ", authCookies.Select(c => $"{c.Key}={c.Value}"));
                request.Headers.Add("Cookie", cookieHeader);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
