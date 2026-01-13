using System.Net.Http.Json;

namespace fencemark.Client.Features.Jobs;

/// <summary>
/// Client service for job-related API operations
/// </summary>
public class JobApiClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("fencemark.ApiService");

    public async Task<IEnumerable<JobDto>?> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/jobs", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<IEnumerable<JobDto>>(cancellationToken);
        }
        return null;
    }

    public async Task<JobDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var response = await _httpClient.GetAsync($"/api/jobs/{id}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<JobDto>(cancellationToken);
        }
        return null;
    }

    public async Task<JobDto?> CreateAsync(JobDto job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var response = await _httpClient.PostAsJsonAsync("/api/jobs", job, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<JobDto>(cancellationToken);
        }
        return null;
    }

    public async Task<JobDto?> UpdateAsync(string id, JobDto job, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(job);

        var response = await _httpClient.PutAsJsonAsync($"/api/jobs/{id}", job, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<JobDto>(cancellationToken);
        }
        return null;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var response = await _httpClient.DeleteAsync($"/api/jobs/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
