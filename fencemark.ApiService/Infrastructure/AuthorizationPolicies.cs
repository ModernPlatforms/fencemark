using Microsoft.AspNetCore.Authorization;

namespace fencemark.ApiService.Infrastructure;

/// <summary>
/// Centralizes the authorization policy names and role requirements used across endpoints,
/// so the same definition is used both when registering policies and when testing them.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy for destructive or organization-management operations that should be
    /// restricted to organization Owners and Admins (not regular Members).
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Roles satisfying the <see cref="AdminOnly"/> policy.
    /// </summary>
    public static readonly string[] AdminOnlyRoles = ["Owner", "Admin"];

    /// <summary>
    /// Registers all fencemark authorization policies on the given options.
    /// </summary>
    public static void AddFencemarkPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy => policy.RequireAuthenticatedUser().RequireRole(AdminOnlyRoles));
    }
}
