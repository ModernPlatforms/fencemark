namespace fencemark.Tests.E2E;

/// <summary>
/// Configuration helper for E2E tests
/// Reads test user credentials from environment variables or uses defaults
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Base URL for the application under test
    /// Can be set via TEST_BASE_URL environment variable
    /// </summary>
    public static string BaseUrl => 
        UrlHelper.NormalizeUrl(
            Environment.GetEnvironmentVariable("TEST_BASE_URL"), 
            "https://localhost:7074");

    /// <summary>
    /// Test user email
    /// REQUIRED: Must be set via TEST_USER_EMAIL environment variable
    /// This should be a persistent test user account in the dev environment
    /// </summary>
    public static string TestUserEmail => 
        Environment.GetEnvironmentVariable("TEST_USER_EMAIL") 
        ?? throw new InvalidOperationException("TEST_USER_EMAIL environment variable must be set");

    /// <summary>
    /// Test user password
    /// REQUIRED: Must be set via TEST_USER_PASSWORD environment variable
    /// Should be stored in Azure Key Vault for dev/staging/prod
    /// </summary>
    public static string TestUserPassword => 
        Environment.GetEnvironmentVariable("TEST_USER_PASSWORD") 
        ?? throw new InvalidOperationException("TEST_USER_PASSWORD environment variable must be set");

    /// <summary>
    /// Whether to run in headless mode
    /// Can be set via TEST_HEADLESS environment variable
    /// </summary>
    public static bool Headless => 
        Environment.GetEnvironmentVariable("TEST_HEADLESS")?.ToLower() != "false";

    /// <summary>
    /// Whether to cleanup test data after test run
    /// Can be set via TEST_CLEANUP environment variable
    /// </summary>
    public static bool CleanupAfterTest => 
        Environment.GetEnvironmentVariable("TEST_CLEANUP")?.ToLower() != "false";
}
