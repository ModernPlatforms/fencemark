using System.Net.Http.Json;

namespace fencemark.Client.Features.GatePositions;

public class GatePositionApiClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("fencemark.ApiService");

    public async Task<IEnumerable<GatePositionDto>?> GetAllAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<GatePositionDto>>("/api/gate-positions");
    }

    public async Task<IEnumerable<GatePositionDto>?> GetBySegmentAsync(string segmentId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<GatePositionDto>>($"/api/gate-positions/by-segment/{segmentId}");
    }

    public async Task<GatePositionDto?> GetByIdAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<GatePositionDto>($"/api/gate-positions/{id}");
    }

    public async Task<GatePositionDto?> CreateAsync(GatePositionDto position)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/gate-positions", position);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GatePositionDto>();
    }

    public async Task<GatePositionDto?> UpdateAsync(string id, GatePositionDto position)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/gate-positions/{id}", position);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GatePositionDto>();
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"/api/gate-positions/{id}");
        response.EnsureSuccessStatusCode();
    }
}
