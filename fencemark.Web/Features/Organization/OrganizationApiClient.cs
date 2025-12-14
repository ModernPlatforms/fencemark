using System.Net.Http.Json;
using fencemark.Web.Features.Auth;

namespace fencemark.Web.Features.Organization;

public record OrganizationMemberResponse(
    string UserId,
    string Email,
    string Role,
    DateTime JoinedAt,
    bool IsGuest);

/// <summary>
/// Client service for organization operations
/// </summary>
public class OrganizationApiClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("API");

    public async Task<IEnumerable<OrganizationMemberResponse>?> GetMembersAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);

        var response = await _httpClient.GetAsync($"/api/organizations/{organizationId}/members", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<IEnumerable<OrganizationMemberResponse>>(cancellationToken);
        }
        return null;
    }

    public async Task<InviteUserResponse?> InviteUserAsync(
        string organizationId,
        InviteUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentNullException.ThrowIfNull(request);

        var response = await _httpClient.PostAsJsonAsync($"/api/organizations/{organizationId}/invite", request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<InviteUserResponse>(cancellationToken);
    }

    public async Task<AuthResponse?> AcceptInvitationAsync(
        AcceptInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await _httpClient.PostAsJsonAsync("/api/organizations/accept-invitation", request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
    }

    public async Task<bool> UpdateRoleAsync(
        string organizationId,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentNullException.ThrowIfNull(request);

        var response = await _httpClient.PutAsJsonAsync($"/api/organizations/{organizationId}/members/role", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveMemberAsync(
        string organizationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var response = await _httpClient.DeleteAsync($"/api/organizations/{organizationId}/members/{userId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
