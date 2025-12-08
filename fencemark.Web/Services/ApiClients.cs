using System.Net.Http.Json;

namespace fencemark.Web.Services;

/// <summary>
/// DTOs for authentication (matching API models)
/// </summary>
public record RegisterRequest(string Email, string Password, string OrganizationName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(bool Success, string? Message, string? UserId, string? OrganizationId, string? Email, bool IsGuest);
public record InviteUserRequest(string Email, string Role);
public record InviteUserResponse(bool Success, string? Message, string? InvitationToken);
public record AcceptInvitationRequest(string Token, string Password);
public record UpdateRoleRequest(string UserId, string Role);

public record OrganizationMemberResponse(
    string UserId,
    string Email,
    string Role,
    DateTime JoinedAt,
    bool IsGuest);

/// <summary>
/// Client service for authentication operations
/// </summary>
public class AuthApiClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("API");

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        }
        return null;
    }

    public async Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("/api/auth/logout", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<object?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/auth/me", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<object>(cancellationToken);
        }
        return null;
    }
}

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
