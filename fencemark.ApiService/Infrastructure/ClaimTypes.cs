namespace fencemark.ApiService.Infrastructure;

/// <summary>
/// Custom claim types used throughout the application
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>
    /// Claim type for the ASP.NET Identity ApplicationUser.Id
    /// This is added in OnTokenValidated to map JWT oid/sub claims to the database user ID
    /// </summary>
    public const string ApplicationUserId = "ApplicationUserId";

    /// <summary>
    /// Claim type for the user's OrganizationId
    /// This is added in OnTokenValidated after looking up the user's organization membership
    /// </summary>
    public const string OrganizationId = "OrganizationId";
}
