namespace fencemark.ApiService.Infrastructure;

/// <summary>
/// Custom claim types used throughout the application
/// These use namespaced URIs to avoid conflicts with standard claims or claims from other systems
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>
    /// Base URI for custom claims
    /// </summary>
    private const string ClaimTypeBaseUri = "https://fencemark.app/claims";

    /// <summary>
    /// Claim type for the ASP.NET Identity ApplicationUser.Id
    /// This is added in OnTokenValidated to map JWT oid/sub claims to the database user ID
    /// </summary>
    public const string ApplicationUserId = $"{ClaimTypeBaseUri}/applicationuserid";

    /// <summary>
    /// Claim type for the user's OrganizationId
    /// This is added in OnTokenValidated after looking up the user's organization membership
    /// </summary>
    public const string OrganizationId = $"{ClaimTypeBaseUri}/organizationid";
}
