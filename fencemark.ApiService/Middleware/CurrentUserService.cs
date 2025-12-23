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
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? UserId => 
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("oid")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email => 
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("preferred_username")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? OrganizationId
    {
        get
        {
            // Get OrganizationId from claims (no DB query, prevents deadlocks)
            // The OrganizationId claim is added during login/registration
            var orgId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("OrganizationId");
            
            if (string.IsNullOrEmpty(orgId))
            {
                var email = Email;
                var allClaims = _httpContextAccessor.HttpContext?.User?.Claims
                    .Select(c => $"{c.Type}={c.Value}")
                    .ToList();
                _logger.LogWarning(
                    "CurrentUserService - OrganizationId claim not found! Email: {Email}, IsAuthenticated: {IsAuth}, Claims: [{Claims}]",
                    email,
                    IsAuthenticated,
                    string.Join(", ", allClaims ?? new List<string>())
                );
            }
            else
            {
                _logger.LogInformation("CurrentUserService - OrganizationId: {OrgId}, Email: {Email}", orgId, Email);
            }
            
            return orgId;
        }
    }
}
