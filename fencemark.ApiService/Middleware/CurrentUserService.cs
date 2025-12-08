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
public class CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context) : ICurrentUserService
{
    private string? _organizationId;
    private bool _organizationIdLoaded;

    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? OrganizationId
    {
        get
        {
            if (!_organizationIdLoaded && UserId is not null)
            {
                _organizationId = context.OrganizationMembers
                    .Where(m => m.UserId == UserId)
                    .Select(m => m.OrganizationId)
                    .FirstOrDefault();
                _organizationIdLoaded = true;
            }
            return _organizationId;
        }
    }
}
