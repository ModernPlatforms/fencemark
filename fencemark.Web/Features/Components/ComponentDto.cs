namespace fencemark.Web.Features.Components;

/// <summary>
/// Data transfer object for component information
/// </summary>
public class ComponentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "Each";
    public decimal UnitPrice { get; set; }
    public string? Material { get; set; }
    public string? Dimensions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
