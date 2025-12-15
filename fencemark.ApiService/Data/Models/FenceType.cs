namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a type of fence that can be installed
/// </summary>
public class FenceType : IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for the fence type
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the fence type (e.g., "6ft Privacy Fence", "4ft Chain Link")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Detailed description of the fence type
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Organization that owns this fence type
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Height of the fence in feet
    /// </summary>
    public decimal HeightInFeet { get; set; }

    /// <summary>
    /// Material of the fence (e.g., "Wood", "Vinyl", "Chain Link", "Aluminum")
    /// </summary>
    public string? Material { get; set; }

    /// <summary>
    /// Style of the fence (e.g., "Privacy", "Picket", "Split Rail")
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Base price per linear foot (labor not included)
    /// </summary>
    public decimal PricePerLinearFoot { get; set; }

    /// <summary>
    /// When the fence type was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the fence type was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the organization
    /// </summary>
    public Organization? Organization { get; set; }

    /// <summary>
    /// Navigation property for fence components
    /// </summary>
    public ICollection<FenceComponent> Components { get; set; } = [];
}
