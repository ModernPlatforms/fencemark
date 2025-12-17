using System.Security.Claims;

namespace fencemark.ApiService.Middleware;

/// <summary>
/// Provides access to the current user's context including organization information
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? OrganizationId { get; }
    bool IsAuthenticated { get; }
}

/// <summary>
/// Implementation of current user service that resolves user context from HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? OrganizationId
    {
        get
        {
            // Get OrganizationId from claims (no DB query, prevents deadlocks)
            // The OrganizationId claim is added during login/registration
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue("OrganizationId");
        }
    }
}
