using System.Net.Http.Json;

namespace fencemark.Web.Features.Components;

/// <summary>
/// Client service for component-related API operations
/// </summary>
public class ComponentApiClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("API");

    public async Task<IEnumerable<ComponentDto>?> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/components", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<IEnumerable<ComponentDto>>(cancellationToken);
        }
        return null;
    }

    public async Task<ComponentDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var response = await _httpClient.GetAsync($"/api/components/{id}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ComponentDto>(cancellationToken);
        }
        return null;
    }

    public async Task<ComponentDto?> CreateAsync(ComponentDto component, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(component);

        var response = await _httpClient.PostAsJsonAsync("/api/components", component, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ComponentDto>(cancellationToken);
        }
        return null;
    }

    public async Task<ComponentDto?> UpdateAsync(string id, ComponentDto component, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(component);

        var response = await _httpClient.PutAsJsonAsync($"/api/components/{id}", component, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ComponentDto>(cancellationToken);
        }
        return null;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var response = await _httpClient.DeleteAsync($"/api/components/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
