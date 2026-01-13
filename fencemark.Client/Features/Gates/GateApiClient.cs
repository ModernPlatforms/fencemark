using System.Net.Http.Json;

namespace fencemark.Client.Features.Gates;

/// <summary>
/// Client service for gate-related API operations
/// </summary>
public class GateApiClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("fencemark.ApiService");

    public async Task<IEnumerable<GateTypeDto>?> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/gates", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<IEnumerable<GateTypeDto>>(cancellationToken);
        }
        return null;
    }

    public async Task<GateTypeDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var response = await _httpClient.GetAsync($"/api/gates/{id}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GateTypeDto>(cancellationToken);
        }
        return null;
    }

    public async Task<GateTypeDto?> CreateAsync(GateTypeDto gate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gate);

        var response = await _httpClient.PostAsJsonAsync("/api/gates", gate, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GateTypeDto>(cancellationToken);
        }
        return null;
    }

    public async Task<GateTypeDto?> UpdateAsync(string id, GateTypeDto gate, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(gate);

        var response = await _httpClient.PutAsJsonAsync($"/api/gates/{id}", gate, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GateTypeDto>(cancellationToken);
        }
        return null;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var response = await _httpClient.DeleteAsync($"/api/gates/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
