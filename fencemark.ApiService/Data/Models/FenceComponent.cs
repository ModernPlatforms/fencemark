namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Links components to fence types with quantity requirements
/// </summary>
public class FenceComponent
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Fence type ID
    /// </summary>
    public required string FenceTypeId { get; set; }

    /// <summary>
    /// Component ID
    /// </summary>
    public required string ComponentId { get; set; }

    /// <summary>
    /// Quantity needed per linear foot of fence
    /// </summary>
    public decimal QuantityPerLinearFoot { get; set; }

    /// <summary>
    /// Navigation property for the fence type
    /// </summary>
    public FenceType? FenceType { get; set; }

    /// <summary>
    /// Navigation property for the component
    /// </summary>
    public Component? Component { get; set; }
}
