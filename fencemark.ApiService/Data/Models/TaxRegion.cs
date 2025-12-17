namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a tax region/jurisdiction with specific tax rates
/// </summary>
public class TaxRegion : IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for the tax region
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Organization that owns this tax region
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Name of the tax region (e.g., "California", "New South Wales", "Ontario")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Region code or abbreviation (e.g., "CA", "NSW", "ON")
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Tax rate as a decimal (e.g., 0.0875 for 8.75%)
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Description of the tax region
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is the default tax region for the organization
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// When the tax region was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the tax region was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the organization
    /// </summary>
    public Organization? Organization { get; set; }
}
