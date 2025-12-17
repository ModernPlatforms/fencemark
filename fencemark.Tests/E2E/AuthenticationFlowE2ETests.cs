using Microsoft.Playwright;

namespace fencemark.Tests.E2E;

/// <summary>
/// E2E tests for Authentication workflow using Playwright
/// Tests login, logout, and session management with persistent test user
/// </summary>
public class AuthenticationFlowE2ETests : PlaywrightTestBase
{
    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AuthenticationFlowE2ETests")]
    public async Task CanLoginWithTestUser()
    {
        // Arrange
        await SetupAsync();
        var authHelper = new PlaywrightAuthHelper(Page!, BaseUrl);

        try
        {
            // Act - Login with persistent test user
            var loginSuccess = await authHelper.LoginAsync(
                TestConfiguration.TestUserEmail, 
                TestConfiguration.TestUserPassword);

            // Assert
            Assert.True(loginSuccess, "Login should succeed with test user credentials");

            // Verify logged in and has organization
            var (userId, userEmail, orgId) = await authHelper.GetCurrentUserAsync();
            Assert.NotNull(userId);
            Assert.NotNull(userEmail);
            Assert.NotNull(orgId);

            await TakeScreenshotAsync("test-user-logged-in.png");
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

        try
        {
            // Act - Login
            var loginSuccess = await authHelper.LoginAsync(
                TestConfiguration.TestUserEmail,
                TestConfiguration.TestUserPassword);
            Assert.True(loginSuccess, "Login should succeed");

            // Verify logged in
            var (userId, userEmail, orgId) = await authHelper.GetCurrentUserAsync();
            Assert.NotNull(userId);
            Assert.Equal(TestConfiguration.TestUserEmail, userEmail);
            Assert.NotNull(orgId);

            await TakeScreenshotAsync("user-logged-in.png");

            // Act - Logout
            var logoutSuccess = await authHelper.LogoutAsync();
            Assert.True(logoutSuccess, "Logout should succeed");

            // Verify logged out
            var (loggedOutUserId, _, _) = await authHelper.GetCurrentUserAsync();
            Assert.Null(loggedOutUserId);

            await TakeScreenshotAsync("user-logged-out.png");
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

        try
        {
            // Login with test user
            await authHelper.LoginAsync(
                TestConfiguration.TestUserEmail,
                TestConfiguration.TestUserPassword);

            // Act - Get current user info
            var (userId, userEmail, orgId) = await authHelper.GetCurrentUserAsync();

            // Assert - OrganizationId should be available from claims
            Assert.NotNull(userId);
            Assert.NotNull(userEmail);
            Assert.NotNull(orgId);

            await TakeScreenshotAsync("org-id-available.png");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    protected override string BaseUrl => TestConfiguration.BaseUrl;
    protected override bool Headless => TestConfiguration.Headless;
}
