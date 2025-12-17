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
    /// Preferred unit system for the organization (Imperial or Metric)
    /// </summary>
    public UnitSystem UnitSystem { get; set; } = UnitSystem.Imperial;

    /// <summary>
    /// Default tax region ID for the organization (optional)
    /// </summary>
    public string? DefaultTaxRegionId { get; set; }

    /// <summary>
    /// Navigation property for organization members
    /// </summary>
    public ICollection<OrganizationMember> Members { get; set; } = [];
}
