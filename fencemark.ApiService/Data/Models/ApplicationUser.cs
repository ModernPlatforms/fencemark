using Microsoft.AspNetCore.Identity;

namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Application user entity extending ASP.NET Core Identity
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Indicates if the user's email has been verified
    /// </summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// Indicates if the user is in guest status (awaiting verification)
    /// </summary>
    public bool IsGuest { get; set; } = true;

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for organization memberships
    /// </summary>
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = [];
}
