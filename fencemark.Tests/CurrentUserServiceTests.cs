using System.Security.Claims;
using fencemark.ApiService.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fencemark.Tests;

public class CurrentUserServiceTests
{
    private static CurrentUserService CreateService(ClaimsPrincipal? principal)
    {
        var accessor = new HttpContextAccessor();
        if (principal != null)
        {
            accessor.HttpContext = new DefaultHttpContext { User = principal };
        }

        var logger = new Logger<CurrentUserService>(new LoggerFactory());
        return new CurrentUserService(accessor, logger);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
    }

    [Fact]
    public void Role_WithRoleClaim_ReturnsRole()
    {
        var principal = CreateAuthenticatedPrincipal(new Claim(ClaimTypes.Role, "Admin"));
        var service = CreateService(principal);

        Assert.Equal("Admin", service.Role);
    }

    [Fact]
    public void Role_WithoutRoleClaim_ReturnsNull()
    {
        var principal = CreateAuthenticatedPrincipal(new Claim(ClaimTypes.Name, "test@example.com"));
        var service = CreateService(principal);

        Assert.Null(service.Role);
    }

    [Fact]
    public void Role_WithNoHttpContext_ReturnsNull()
    {
        var service = CreateService(null);

        Assert.Null(service.Role);
    }

    [Fact]
    public void IsAuthenticated_WithAuthenticatedIdentity_ReturnsTrue()
    {
        var principal = CreateAuthenticatedPrincipal(new Claim(ClaimTypes.Role, "Owner"));
        var service = CreateService(principal);

        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_WithNoHttpContext_ReturnsFalse()
    {
        var service = CreateService(null);

        Assert.False(service.IsAuthenticated);
    }
}
