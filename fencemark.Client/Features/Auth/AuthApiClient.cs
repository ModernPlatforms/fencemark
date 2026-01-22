using System.Net.Http.Json;

namespace fencemark.Client.Features.Auth;

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
public record CurrentUserResponse(
    string? UserId,
    string? Email,
    string? UserName,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? OrganizationId,
    string? OrganizationName,
    bool IsEmailVerified,
    bool IsGuest,
    DateTime? CreatedAt);
public record UpdateUserRequest(string? Email, string? FirstName, string? LastName, string? CurrentPassword, string? NewPassword);
public record UpdateUserResponse(bool Success, string? Message, string? UserId, string? Email, string? FirstName, string? LastName, string? DisplayName);

/// <summary>
/// Client service for authentication operations
/// </summary>
public class AuthApiClient(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("fencemark.ApiService");

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

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/auth/me", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CurrentUserResponse>(cancellationToken);
        }
        return null;
    }

    public async Task<UpdateUserResponse?> UpdateCurrentUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await _httpClient.PutAsJsonAsync("/api/auth/me", request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<UpdateUserResponse>(cancellationToken);
    }

    public async Task<bool> DeleteAccountAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync("/api/auth/account", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

