using System.Net.Http.Json;

namespace fencemark.Client.Features.FenceSegments;

public class FenceSegmentApiClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("fencemark.ApiService");

    public async Task<IEnumerable<FenceSegmentDto>?> GetAllAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<FenceSegmentDto>>("/api/fence-segments");
    }

    public async Task<IEnumerable<FenceSegmentDto>?> GetByJobAsync(string jobId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<FenceSegmentDto>>($"/api/fence-segments/by-job/{jobId}");
    }

    public async Task<IEnumerable<FenceSegmentDto>?> GetByParcelAsync(string parcelId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<FenceSegmentDto>>($"/api/fence-segments/by-parcel/{parcelId}");
    }

    public async Task<FenceSegmentDto?> GetByIdAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<FenceSegmentDto>($"/api/fence-segments/{id}");
    }

    public async Task<FenceSegmentDto?> CreateAsync(FenceSegmentDto segment)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/fence-segments", segment);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FenceSegmentDto>();
    }

    public async Task<FenceSegmentDto?> UpdateAsync(string id, FenceSegmentDto segment)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/fence-segments/{id}", segment);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FenceSegmentDto>();
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"/api/fence-segments/{id}");
        response.EnsureSuccessStatusCode();
    }
}
