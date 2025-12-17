using Microsoft.Playwright;
using System.Text;
using System.Text.Json;

namespace fencemark.Tests.E2E;

/// <summary>
/// Helper class for authentication-related operations in Playwright tests
/// </summary>
public class PlaywrightAuthHelper
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public PlaywrightAuthHelper(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Register a new user with organization
    /// </summary>
    public async Task<(bool success, string? userId, string? organizationId)> RegisterAsync(
        string email,
        string password,
        string organizationName)
    {
        var payload = JsonSerializer.Serialize(new
        {
            email,
            password,
            organizationName
        });

        var response = await _page.EvaluateAsync<JsonElement>(@"
            async (args) => {
                const response = await fetch(args.url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: args.body,
                    credentials: 'include'
                });
                return await response.json();
            }", new { url = $"{_baseUrl}/api/auth/register", body = payload });

        var success = response.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        var userId = response.TryGetProperty("userId", out var userIdProp) ? userIdProp.GetString() : null;
        var organizationId = response.TryGetProperty("organizationId", out var orgIdProp) ? orgIdProp.GetString() : null;

        return (success, userId, organizationId);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        var payload = JsonSerializer.Serialize(new
        {
            email,
            password
        });

        var response = await _page.EvaluateAsync<JsonElement>(@"
            async (args) => {
                const response = await fetch(args.url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: args.body,
                    credentials: 'include'
                });
                return { ok: response.ok, status: response.status };
            }", new { url = $"{_baseUrl}/api/auth/login", body = payload });

        return response.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    public async Task<bool> LogoutAsync()
    {
        var response = await _page.EvaluateAsync<JsonElement>(@"
            async (url) => {
                const response = await fetch(url, {
                    method: 'POST',
                    credentials: 'include'
                });
                return { ok: response.ok };
            }", $"{_baseUrl}/api/auth/logout");

        return response.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
    }

    /// <summary>
    /// Delete current user account
    /// </summary>
    public async Task<bool> DeleteAccountAsync()
    {
        var response = await _page.EvaluateAsync<JsonElement>(@"
            async (url) => {
                const response = await fetch(url, {
                    method: 'DELETE',
                    credentials: 'include'
                });
                return { ok: response.ok };
            }", $"{_baseUrl}/api/auth/account");

        return response.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    public async Task<(string? userId, string? email, string? organizationId)> GetCurrentUserAsync()
    {
        var response = await _page.EvaluateAsync<JsonElement>(@"
            async (url) => {
                const response = await fetch(url, {
                    method: 'GET',
                    credentials: 'include'
                });
                if (response.ok) {
                    return await response.json();
                }
                return {};
            }", $"{_baseUrl}/api/auth/me");

        var userId = response.TryGetProperty("userId", out var userIdProp) ? userIdProp.GetString() : null;
        var email = response.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
        var organizationId = response.TryGetProperty("organizationId", out var orgIdProp) ? orgIdProp.GetString() : null;

        return (userId, email, organizationId);
    }
}
