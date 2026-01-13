using System.Net.Http.Json;

namespace fencemark.Client.Features.Fences;

/// <summary>
/// Client service for fence-related API operations
/// </summary>
public class FenceApiClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("fencemark.ApiService");

    public async Task<IEnumerable<FenceTypeDto>?> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/fences", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<IEnumerable<FenceTypeDto>>(cancellationToken);
        }
        return null;
    }

    public async Task<FenceTypeDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var response = await _httpClient.GetAsync($"/api/fences/{id}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<FenceTypeDto>(cancellationToken);
        }
        return null;
    }

    public async Task<FenceTypeDto?> CreateAsync(FenceTypeDto fence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fence);

        var response = await _httpClient.PostAsJsonAsync("/api/fences", fence, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<FenceTypeDto>(cancellationToken);
        }
        return null;
    }

    public async Task<FenceTypeDto?> UpdateAsync(string id, FenceTypeDto fence, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(fence);

        var response = await _httpClient.PutAsJsonAsync($"/api/fences/{id}", fence, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<FenceTypeDto>(cancellationToken);
        }
        return null;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var response = await _httpClient.DeleteAsync($"/api/fences/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
