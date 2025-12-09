namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Links components to gate types with quantity requirements
/// </summary>
public class GateComponent
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gate type ID
    /// </summary>
    public required string GateTypeId { get; set; }

    /// <summary>
    /// Component ID
    /// </summary>
    public required string ComponentId { get; set; }

    /// <summary>
    /// Quantity needed per gate
    /// </summary>
    public decimal QuantityPerGate { get; set; }

    /// <summary>
    /// Navigation property for the gate type
    /// </summary>
    public GateType? GateType { get; set; }

    /// <summary>
    /// Navigation property for the component
    /// </summary>
    public Component? Component { get; set; }
}
