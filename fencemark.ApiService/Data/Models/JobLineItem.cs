namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a line item in a job (fence or gate)
/// </summary>
public class JobLineItem
{
    /// <summary>
    /// Unique identifier for the line item
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Job ID
    /// </summary>
    public required string JobId { get; set; }

    /// <summary>
    /// Type of line item
    /// </summary>
    public LineItemType ItemType { get; set; }

    /// <summary>
    /// Fence type ID (if applicable)
    /// </summary>
    public string? FenceTypeId { get; set; }

    /// <summary>
    /// Gate type ID (if applicable)
    /// </summary>
    public string? GateTypeId { get; set; }

    /// <summary>
    /// Description of the line item
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Quantity (linear feet for fences, count for gates)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total price for this line item (quantity * unit price)
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Navigation property for the job
    /// </summary>
    public Job? Job { get; set; }

    /// <summary>
    /// Navigation property for the fence type
    /// </summary>
    public FenceType? FenceType { get; set; }

    /// <summary>
    /// Navigation property for the gate type
    /// </summary>
    public GateType? GateType { get; set; }
}

/// <summary>
/// Type of line item in a job
/// </summary>
public enum LineItemType
{
    Fence,
    Gate,
    Labor,
    Other
}
