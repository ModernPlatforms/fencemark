using System.Security.Claims;
using fencemark.ApiService.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace fencemark.Tests;

/// <summary>
/// Exercises the real ASP.NET Core authorization pipeline wired up exactly as Program.cs
/// configures it, to guard against the AdminOnly policy silently allowing non-admin roles.
/// </summary>
public class AuthorizationPoliciesTests
{
    private static IAuthorizationService CreateAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options => options.AddFencemarkPolicies());
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal CreatePrincipal(string? role)
    {
        var claims = new List<Claim>();
        if (role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    public async Task AdminOnlyPolicy_WithOwnerOrAdminRole_Succeeds(string role)
    {
        var authorizationService = CreateAuthorizationService();
        var principal = CreatePrincipal(role);

        var result = await authorizationService.AuthorizeAsync(principal, AuthorizationPolicies.AdminOnly);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Billing")]
    [InlineData("ReadOnly")]
    public async Task AdminOnlyPolicy_WithNonAdminRole_Fails(string role)
    {
        var authorizationService = CreateAuthorizationService();
        var principal = CreatePrincipal(role);

        var result = await authorizationService.AuthorizeAsync(principal, AuthorizationPolicies.AdminOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AdminOnlyPolicy_WithNoRoleClaim_Fails()
    {
        var authorizationService = CreateAuthorizationService();
        var principal = CreatePrincipal(role: null);

        var result = await authorizationService.AuthorizeAsync(principal, AuthorizationPolicies.AdminOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AdminOnlyPolicy_WithUnauthenticatedPrincipal_Fails()
    {
        var authorizationService = CreateAuthorizationService();
        // No authenticationType => IsAuthenticated is false, mirroring an anonymous request.
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Owner") }));

        var result = await authorizationService.AuthorizeAsync(principal, AuthorizationPolicies.AdminOnly);

        Assert.False(result.Succeeded);
    }
}
