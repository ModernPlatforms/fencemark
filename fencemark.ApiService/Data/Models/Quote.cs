namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a quote for a job
/// </summary>
public class Quote : IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for the quote
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Job this quote is for
    /// </summary>
    public required string JobId { get; set; }

    /// <summary>
    /// Organization that owns this quote
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Pricing configuration used for this quote
    /// </summary>
    public string? PricingConfigId { get; set; }

    /// <summary>
    /// Quote number (for display purposes)
    /// </summary>
    public required string QuoteNumber { get; set; }

    /// <summary>
    /// Current version number
    /// </summary>
    public int CurrentVersion { get; set; } = 1;

    /// <summary>
    /// Status of the quote
    /// </summary>
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;

    /// <summary>
    /// Total materials cost
    /// </summary>
    public decimal MaterialsCost { get; set; }

    /// <summary>
    /// Total labor cost
    /// </summary>
    public decimal LaborCost { get; set; }

    /// <summary>
    /// Subtotal before contingency and profit
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Contingency amount
    /// </summary>
    public decimal ContingencyAmount { get; set; }

    /// <summary>
    /// Profit margin amount
    /// </summary>
    public decimal ProfitAmount { get; set; }

    /// <summary>
    /// Total quote amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Tax amount (if applicable)
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Grand total including tax
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Valid until date
    /// </summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Terms and conditions
    /// </summary>
    public string? Terms { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When the quote was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the quote was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the job
    /// </summary>
    public Job? Job { get; set; }

    /// <summary>
    /// Navigation property for the organization
    /// </summary>
    public Organization? Organization { get; set; }

    /// <summary>
    /// Navigation property for the pricing configuration
    /// </summary>
    public PricingConfig? PricingConfig { get; set; }

    /// <summary>
    /// Navigation property for quote versions
    /// </summary>
    public ICollection<QuoteVersion> Versions { get; set; } = [];

    /// <summary>
    /// Navigation property for bill of materials items
    /// </summary>
    public ICollection<BillOfMaterialsItem> BillOfMaterials { get; set; } = [];
}

/// <summary>
/// Status of a quote
/// </summary>
public enum QuoteStatus
{
    Draft,
    Pending,
    Sent,
    Accepted,
    Rejected,
    Expired,
    Revised
}
