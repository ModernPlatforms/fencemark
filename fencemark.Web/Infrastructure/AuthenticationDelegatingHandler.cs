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
            // Forward cookies for cookie-based authentication
            // This includes the .AspNetCore.Identity.Application cookie set by the API service
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
