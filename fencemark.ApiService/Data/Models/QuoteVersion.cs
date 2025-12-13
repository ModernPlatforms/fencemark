namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a version of a quote for tracking changes
/// </summary>
public class QuoteVersion
{
    /// <summary>
    /// Unique identifier for the quote version
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Quote this version belongs to
    /// </summary>
    public required string QuoteId { get; set; }

    /// <summary>
    /// Version number
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Summary of changes in this version
    /// </summary>
    public string? ChangeSummary { get; set; }

    /// <summary>
    /// Materials cost in this version
    /// </summary>
    public decimal MaterialsCost { get; set; }

    /// <summary>
    /// Labor cost in this version
    /// </summary>
    public decimal LaborCost { get; set; }

    /// <summary>
    /// Subtotal in this version
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Contingency amount in this version
    /// </summary>
    public decimal ContingencyAmount { get; set; }

    /// <summary>
    /// Profit amount in this version
    /// </summary>
    public decimal ProfitAmount { get; set; }

    /// <summary>
    /// Total amount in this version
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Tax amount in this version
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Grand total in this version
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Snapshot of the BOM as JSON
    /// </summary>
    public string? BomSnapshot { get; set; }

    /// <summary>
    /// Snapshot of pricing configuration as JSON
    /// </summary>
    public string? PricingConfigSnapshot { get; set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this version
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Navigation property for the quote
    /// </summary>
    public Quote? Quote { get; set; }
}
