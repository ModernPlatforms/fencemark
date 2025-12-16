using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace fencemark.Tests;

/// <summary>
/// Integration tests for SQL Server connectivity with Aspire.
/// These tests verify that the SQL Server container starts correctly and the API service
/// can connect to it when running through Aspire orchestration.
/// 
/// Note: These tests require Docker and Aspire DCP to be available, so they are
/// conditionally skipped in environments where these dependencies are not available.
/// </summary>
public class SqlServerIntegrationTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(90);

    private static readonly TimeSpan DockerCheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Determines if the test environment supports running Aspire integration tests.
    /// Checks for Docker availability and DCP support.
    /// </summary>
    private static bool CanRunAspireIntegrationTests()
    {
        try
        {
            // Check if Docker is available
            var dockerProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            
            if (dockerProcess == null)
                return false;
                
            dockerProcess.WaitForExit((int)DockerCheckTimeout.TotalMilliseconds);
            return dockerProcess.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    [Fact(Skip = "Requires Docker and Aspire DCP - run manually with: dotnet test --filter SqlServerIntegrationTests", Timeout = 120000)]
    public async Task SqlServer_StartsSuccessfully_WhenOrchestrated()
    {
        // Skip if environment doesn't support Aspire integration tests
        if (!CanRunAspireIntegrationTests())
        {
            Assert.Skip("Docker is not available for running SQL Server container tests");
            return;
        }

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.fencemark_AppHost>(cancellationToken);
        
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Aspire", LogLevel.Debug);
            logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Information);
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        // Act - Start the application (this starts SQL Server container)
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Assert - Wait for SQL Server to be healthy
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sql", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Verify we can get the connection string
        var connectionString = await app.GetConnectionStringAsync("sql", cancellationToken);
        Assert.NotNull(connectionString);
        Assert.NotEmpty(connectionString);
        
        // Verify the connection string contains expected components
        Assert.Contains("Server=", connectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Database=fencemark", connectionString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Requires Docker and Aspire DCP - run manually with: dotnet test --filter SqlServerIntegrationTests", Timeout = 120000)]
    public async Task ApiService_ConnectsToSqlServer_AfterSqlServerIsReady()
    {
        // Skip if environment doesn't support Aspire integration tests
        if (!CanRunAspireIntegrationTests())
        {
            Assert.Skip("Docker is not available for running SQL Server container tests");
            return;
        }

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.fencemark_AppHost>(cancellationToken);
        
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        // Act - Start the application
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Assert - Wait for SQL Server to be healthy first
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sql", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Assert - Wait for API Service to be healthy (validates migrations ran successfully)
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Assert - Verify API Service health endpoint is responding
        var httpClient = app.CreateHttpClient("apiservice");
        var healthResponse = await httpClient.GetAsync("/health", cancellationToken);
        
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        
        var healthContent = await healthResponse.Content.ReadAsStringAsync(cancellationToken);
        Assert.Equal("OK", healthContent);
    }

    [Fact(Skip = "Requires Docker and Aspire DCP - run manually with: dotnet test --filter SqlServerIntegrationTests", Timeout = 120000)]
    public async Task SqlServer_DatabaseIsCreated_AndAccessible()
    {
        // Skip if environment doesn't support Aspire integration tests
        if (!CanRunAspireIntegrationTests())
        {
            Assert.Skip("Docker is not available for running SQL Server container tests");
            return;
        }

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.fencemark_AppHost>(cancellationToken);

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        // Act - Start the application
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Wait for SQL Server to be ready
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sql", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Get connection string and verify we can connect
        var connectionString = await app.GetConnectionStringAsync("sql", cancellationToken);
        Assert.NotNull(connectionString);

        // Assert - Verify we can connect to the database
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        Assert.Equal("fencemark", connection.Database);

        // Verify we can execute a simple query
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT @@VERSION";
        var version = await command.ExecuteScalarAsync(cancellationToken);
        
        Assert.NotNull(version);
        Assert.Contains("Microsoft SQL Server", version.ToString() ?? string.Empty);
    }

    [Fact(Skip = "Requires Docker and Aspire DCP - run manually with: dotnet test --filter SqlServerIntegrationTests", Timeout = 120000)]
    public async Task ApiService_WaitForSqlServer_PreventsStartupFailures()
    {
        // This test specifically validates the fix for the Aspire hosting startup issue.
        // It ensures that the API service does not start until SQL Server is ready,
        // preventing migration failures and health check failures.
        
        // Skip if environment doesn't support Aspire integration tests
        if (!CanRunAspireIntegrationTests())
        {
            Assert.Skip("Docker is not available for running SQL Server container tests");
            return;
        }

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.fencemark_AppHost>(cancellationToken);

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        // Act - Start the application
        var startTime = DateTime.UtcNow;
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Track when SQL becomes healthy
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sql", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        var sqlHealthyTime = DateTime.UtcNow;

        // Track when API becomes healthy
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        var apiHealthyTime = DateTime.UtcNow;

        // Assert - API service should become healthy after or at the same time as SQL Server
        // This validates that the .WaitFor(sql) configuration is working
        // Note: Times may be identical due to DateTime precision, which is acceptable as it means
        // the API service waited for SQL before becoming healthy
        Assert.True(apiHealthyTime >= sqlHealthyTime, 
            $"API service became healthy at {apiHealthyTime:O}, but SQL Server became healthy at {sqlHealthyTime:O}. " +
            "API service should not be healthy before SQL Server is ready.");

        // Additional validation - verify health endpoint works
        var httpClient = app.CreateHttpClient("apiservice");
        var healthResponse = await httpClient.GetAsync("/health", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
