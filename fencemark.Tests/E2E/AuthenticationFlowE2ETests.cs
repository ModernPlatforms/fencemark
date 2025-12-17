using Microsoft.Playwright;

namespace fencemark.Tests.E2E;

/// <summary>
/// E2E tests for Authentication workflow using Playwright
/// Tests registration, login, logout, and account deletion
/// </summary>
public class AuthenticationFlowE2ETests : PlaywrightTestBase
{
    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AuthenticationFlowE2ETests")]
    public async Task CanRegisterNewUser()
    {
        // Arrange
        await SetupAsync();
        var authHelper = new PlaywrightAuthHelper(Page!, BaseUrl);
        var email = $"test-{Guid.NewGuid().ToString()[..8]}@test.com";
        var password = "TestPassword123!";
        var orgName = $"Test Org {Guid.NewGuid().ToString()[..8]}";

        try
        {
            // Act
            var (success, userId, organizationId) = await authHelper.RegisterAsync(
                email, password, orgName);

            // Assert
            Assert.True(success, "Registration should succeed");
            Assert.NotNull(userId);
            Assert.NotNull(organizationId);

            await TakeScreenshotAsync("user-registered.png");

            // Cleanup
            await authHelper.LoginAsync(email, password);
            await authHelper.DeleteAccountAsync();
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AuthenticationFlowE2ETests")]
    public async Task CanLoginAndLogout()
    {
        // Arrange
        await SetupAsync();
        var authHelper = new PlaywrightAuthHelper(Page!, BaseUrl);
        var email = $"test-{Guid.NewGuid().ToString()[..8]}@test.com";
        var password = "TestPassword123!";

        try
        {
            // Register user first
            var (regSuccess, _, _) = await authHelper.RegisterAsync(
                email, password, "Test Org");
            Assert.True(regSuccess);

            // Act - Login
            var loginSuccess = await authHelper.LoginAsync(email, password);
            Assert.True(loginSuccess, "Login should succeed");

            // Verify logged in
            var (userId, userEmail, orgId) = await authHelper.GetCurrentUserAsync();
            Assert.NotNull(userId);
            Assert.Equal(email, userEmail);

            await TakeScreenshotAsync("user-logged-in.png");

            // Act - Logout
            var logoutSuccess = await authHelper.LogoutAsync();
            Assert.True(logoutSuccess, "Logout should succeed");

            // Verify logged out
            var (loggedOutUserId, _, _) = await authHelper.GetCurrentUserAsync();
            Assert.Null(loggedOutUserId);

            await TakeScreenshotAsync("user-logged-out.png");

            // Cleanup
            await authHelper.LoginAsync(email, password);
            await authHelper.DeleteAccountAsync();
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AuthenticationFlowE2ETests")]
    public async Task CanDeleteAccount()
    {
        // Arrange
        await SetupAsync();
        var authHelper = new PlaywrightAuthHelper(Page!, BaseUrl);
        var email = $"test-{Guid.NewGuid().ToString()[..8]}@test.com";
        var password = "TestPassword123!";

        try
        {
            // Register and login
            var (regSuccess, _, _) = await authHelper.RegisterAsync(
                email, password, "Test Org");
            Assert.True(regSuccess);

            await authHelper.LoginAsync(email, password);

            // Act - Delete account
            var deleteSuccess = await authHelper.DeleteAccountAsync();

            // Assert
            Assert.True(deleteSuccess, "Account deletion should succeed");

            // Verify user is logged out and cannot login
            var loginAfterDelete = await authHelper.LoginAsync(email, password);
            Assert.False(loginAfterDelete, "Login should fail after account deletion");

            await TakeScreenshotAsync("account-deleted.png");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AuthenticationFlowE2ETests")]
    public async Task CannotRegisterWithDuplicateEmail()
    {
        // Arrange
        await SetupAsync();
        var authHelper = new PlaywrightAuthHelper(Page!, BaseUrl);
        var email = $"test-{Guid.NewGuid().ToString()[..8]}@test.com";
        var password = "TestPassword123!";

        try
        {
            // Register user first
            var (firstSuccess, _, _) = await authHelper.RegisterAsync(
                email, password, "First Org");
            Assert.True(firstSuccess);

            // Act - Try to register again with same email
            var (secondSuccess, _, _) = await authHelper.RegisterAsync(
                email, password, "Second Org");

            // Assert
            Assert.False(secondSuccess, "Second registration with same email should fail");

            await TakeScreenshotAsync("duplicate-email-rejected.png");

            // Cleanup
            await authHelper.LoginAsync(email, password);
            await authHelper.DeleteAccountAsync();
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AuthenticationFlowE2ETests")]
    public async Task OrganizationIdIsAvailableAfterLogin()
    {
        // Arrange
        await SetupAsync();
        var authHelper = new PlaywrightAuthHelper(Page!, BaseUrl);
        var email = $"test-{Guid.NewGuid().ToString()[..8]}@test.com";
        var password = "TestPassword123!";

        try
        {
            // Register user
            var (regSuccess, _, registeredOrgId) = await authHelper.RegisterAsync(
                email, password, "Test Org");
            Assert.True(regSuccess);
            Assert.NotNull(registeredOrgId);

            // Login
            await authHelper.LoginAsync(email, password);

            // Act - Get current user info
            var (userId, userEmail, orgId) = await authHelper.GetCurrentUserAsync();

            // Assert - OrganizationId should be available from claims
            Assert.NotNull(orgId);
            Assert.Equal(registeredOrgId, orgId);

            await TakeScreenshotAsync("org-id-available.png");

            // Cleanup
            await authHelper.DeleteAccountAsync();
        }
        finally
        {
            await TeardownAsync();
        }
    }

    protected override string BaseUrl => TestConfiguration.BaseUrl;
    protected override bool Headless => TestConfiguration.Headless;
}
