namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents an organization (company account) in the system
/// </summary>
public class Organization
{
    /// <summary>
    /// Unique identifier for the organization
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the organization
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// When the organization was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for organization members
    /// </summary>
    public ICollection<OrganizationMember> Members { get; set; } = [];
}
