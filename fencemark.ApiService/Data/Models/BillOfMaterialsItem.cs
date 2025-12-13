namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents an item in a bill of materials
/// </summary>
public class BillOfMaterialsItem
{
    /// <summary>
    /// Unique identifier for the BOM item
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Quote this BOM item belongs to
    /// </summary>
    public required string QuoteId { get; set; }

    /// <summary>
    /// Component this BOM item references (if applicable)
    /// </summary>
    public string? ComponentId { get; set; }

    /// <summary>
    /// Line item category (e.g., "Posts", "Rails", "Panels", "Hardware", "Gates", "Labor")
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Item description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// SKU or product code
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Quantity needed
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure
    /// </summary>
    public string UnitOfMeasure { get; set; } = "Each";

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total price for this line (quantity * unit price)
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Additional notes about this item
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property for the quote
    /// </summary>
    public Quote? Quote { get; set; }

    /// <summary>
    /// Navigation property for the component
    /// </summary>
    public Component? Component { get; set; }
}
