namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Defines the roles a user can have within an organization
/// </summary>
public enum Role
{
    /// <summary>
    /// Organization owner with full permissions
    /// </summary>
    Owner,

    /// <summary>
    /// Administrator with management permissions
    /// </summary>
    Admin,

    /// <summary>
    /// Regular member with standard permissions
    /// </summary>
    Member,

    /// <summary>
    /// Billing manager with access to billing information
    /// </summary>
    Billing,

    /// <summary>
    /// Read-only access to organization data
    /// </summary>
    ReadOnly
}
