using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace fencemark.Web.Infrastructure;

/// <summary>
/// Delegating handler that forwards authentication from Blazor Server to API calls.
/// For Blazor Server, we use a shared cookie between Web and API services.
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
            // For Blazor Server with cookie auth, we need to forward the cookie
            // Get all cookies from the incoming request
            var cookies = httpContext.Request.Cookies;
            
            if (cookies.Any())
            {
                var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}"));
                request.Headers.Add("Cookie", cookieHeader);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
