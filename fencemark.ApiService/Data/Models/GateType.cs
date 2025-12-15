namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a type of gate that can be installed
/// </summary>
public class GateType : IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for the gate type
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the gate type (e.g., "Single Walk Gate", "Double Drive Gate")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Detailed description of the gate type
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Organization that owns this gate type
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Width of the gate in feet
    /// </summary>
    public decimal WidthInFeet { get; set; }

    /// <summary>
    /// Height of the gate in feet
    /// </summary>
    public decimal HeightInFeet { get; set; }

    /// <summary>
    /// Material of the gate (e.g., "Wood", "Vinyl", "Metal", "Aluminum")
    /// </summary>
    public string? Material { get; set; }

    /// <summary>
    /// Style of the gate (e.g., "Walk-through", "Drive-through", "Pedestrian")
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Base price for the gate (including hardware)
    /// </summary>
    public decimal BasePrice { get; set; }

    /// <summary>
    /// When the gate type was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the gate type was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the organization
    /// </summary>
    public Organization? Organization { get; set; }

    /// <summary>
    /// Navigation property for gate components
    /// </summary>
    public ICollection<GateComponent> Components { get; set; } = [];
}
