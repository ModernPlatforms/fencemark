using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace fencemark.Client.Features.Auth;

/// <summary>
/// A simple authentication state provider that always returns an unauthenticated user.
/// Used for local development when Azure AD authentication is not configured.
/// </summary>
public class NoAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _unauthenticatedTask = 
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return _unauthenticatedTask;
    }
}
