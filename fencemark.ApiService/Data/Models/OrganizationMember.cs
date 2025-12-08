namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a user's membership in an organization
/// </summary>
public class OrganizationMember
{
    /// <summary>
    /// Unique identifier for the membership
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID (foreign key)
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Organization ID (foreign key)
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Navigation property to the organization
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// Role of the user within the organization
    /// </summary>
    public Role Role { get; set; } = Role.Member;

    /// <summary>
    /// When the invitation was sent (null if auto-created)
    /// </summary>
    public DateTime? InvitedAt { get; set; }

    /// <summary>
    /// When the user joined the organization
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional invitation token for pending invitations
    /// </summary>
    public string? InvitationToken { get; set; }

    /// <summary>
    /// Indicates if the invitation has been accepted
    /// </summary>
    public bool IsAccepted { get; set; } = true;
}
