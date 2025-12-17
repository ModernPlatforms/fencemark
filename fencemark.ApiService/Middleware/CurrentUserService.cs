using fencemark.ApiService.Data;
using Microsoft.EntityFrameworkCore;
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
    private readonly IServiceScopeFactory _scopeFactory;

    private string? _organizationId;
    private bool _organizationIdLoaded;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IServiceScopeFactory scopeFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? OrganizationId
    {
        get
        {
            // First try to get from claims (fastest, no DB query)
            var organizationIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("OrganizationId");
            if (!string.IsNullOrEmpty(organizationIdClaim))
            {
                return organizationIdClaim;
            }

            // Fallback to database query for backward compatibility (cached)
            if (!_organizationIdLoaded && UserId is not null)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                _organizationId = context.OrganizationMembers
                    .AsNoTracking()
                    .Where(m => m.UserId == UserId)
                    .Select(m => m.OrganizationId)
                    .FirstOrDefaultAsync()
                    .GetAwaiter()
                    .GetResult();
                _organizationIdLoaded = true;
            }
            return _organizationId;
        }
    }
}
