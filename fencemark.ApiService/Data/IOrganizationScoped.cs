namespace fencemark.ApiService.Data;

/// <summary>
/// Interface for entities that are scoped to an organization (multi-tenant isolation)
/// </summary>
public interface IOrganizationScoped
{
    /// <summary>
    /// The organization that owns this entity
    /// </summary>
    string OrganizationId { get; set; }
}
