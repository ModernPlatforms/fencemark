using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace fencemark.Tests;

/// <summary>
/// Integration tests for Row-Level Security (RLS) enforcement at the database level.
/// These tests verify that SQL Server RLS policies correctly filter data based on SESSION_CONTEXT,
/// ensuring multi-tenant data isolation even if application-layer filters are missed.
/// </summary>
public class RlsDatabaseEnforcementTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan DockerCheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Determines if the test environment supports running Aspire integration tests.
    /// </summary>
    private static bool CanRunAspireIntegrationTests()
    {
        try
        {
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

    [Fact(Skip = "Requires Docker and Aspire DCP - run manually with: dotnet test --filter RlsDatabaseEnforcementTests")]
    public async Task RlsSecurityFunction_Exists_InDatabase()
    {
        if (!CanRunAspireIntegrationTests())
        {
            Assert.Skip("Docker is not available for running SQL Server container tests");
            return;
        }

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.fencemark_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sql", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var connectionString = await app.GetConnectionStringAsync("sql", cancellationToken);

        // Act & Assert
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) 
            FROM sys.objects 
            WHERE type = 'IF' AND name = N'fn_SecurityPredicate'";
        
        var count = (int)await command.ExecuteScalarAsync(cancellationToken);
        
        Assert.Equal(1, count);
    }

    [Fact(Skip = "Requires Docker and Aspire DCP - run manually with: dotnet test --filter RlsDatabaseEnforcementTests")]
    public async Task RlsSecurityPolicy_Exists_InDatabase()
    {
        if (!CanRunAspireIntegrationTests())
        {
            Assert.Skip("Docker is not available for running SQL Server container tests");
            return;
        }

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.fencemark_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sql", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var connectionString = await app.GetConnectionStringAsync("sql", cancellationToken);

        // Act & Assert
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) 
            FROM sys.security_policies 
            WHERE name = N'TenantFilterPolicy' AND is_enabled = 1";
        
        var count = (int)await command.ExecuteScalarAsync(cancellationToken);
        
        Assert.Equal(1, count);
    }

    [Fact(Skip = "Requires Docker and Aspire DCP - run manually with: dotnet test --filter RlsDatabaseEnforcementTests")]
    public async Task RlsPolicy_FiltersComponents_BasedOnSessionContext()
    {
        if (!CanRunAspireIntegrationTests())
        {
            Assert.Skip("Docker is not available for running SQL Server container tests");
            return;
        }

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.fencemark_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sql", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var connectionString = await app.GetConnectionStringAsync("sql", cancellationToken);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Create test organizations
        var org1Id = Guid.NewGuid().ToString();
        var org2Id = Guid.NewGuid().ToString();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO Organizations (Id, Name, CreatedAt, UpdatedAt)
                VALUES (@org1, 'Test Org 1', GETUTCDATE(), GETUTCDATE()),
                       (@org2, 'Test Org 2', GETUTCDATE(), GETUTCDATE())";
            cmd.Parameters.AddWithValue("@org1", org1Id);
            cmd.Parameters.AddWithValue("@org2", org2Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Create test components for each organization
        var comp1Id = Guid.NewGuid().ToString();
        var comp2Id = Guid.NewGuid().ToString();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO Components (Id, Name, Category, OrganizationId, CreatedAt, UpdatedAt)
                VALUES (@comp1, 'Org1 Component', 'Post', @org1, GETUTCDATE(), GETUTCDATE()),
                       (@comp2, 'Org2 Component', 'Rail', @org2, GETUTCDATE(), GETUTCDATE())";
            cmd.Parameters.AddWithValue("@comp1", comp1Id);
            cmd.Parameters.AddWithValue("@comp2", comp2Id);
            cmd.Parameters.AddWithValue("@org1", org1Id);
            cmd.Parameters.AddWithValue("@org2", org2Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Act & Assert - Test with Org1 session context
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "EXEC sp_set_session_context @key = N'OrganizationId', @value = @orgId;";
            cmd.Parameters.AddWithValue("@orgId", org1Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Components";
            var count = (int)await cmd.ExecuteScalarAsync(cancellationToken);
            
            // Should only see Org1's component due to RLS
            Assert.Equal(1, count);
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id FROM Components";
            var id = (string)await cmd.ExecuteScalarAsync(cancellationToken);
            
            // Should only see Org1's component
            Assert.Equal(comp1Id, id);
        }

        // Test with Org2 session context
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "EXEC sp_set_session_context @key = N'OrganizationId', @value = @orgId;";
            cmd.Parameters.AddWithValue("@orgId", org2Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Components";
            var count = (int)await cmd.ExecuteScalarAsync(cancellationToken);
            
            // Should only see Org2's component due to RLS
            Assert.Equal(1, count);
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id FROM Components";
            var id = (string)await cmd.ExecuteScalarAsync(cancellationToken);
            
            // Should only see Org2's component
            Assert.Equal(comp2Id, id);
        }
    }

    [Fact(Skip = "Requires Docker and Aspire DCP - run manually with: dotnet test --filter RlsDatabaseEnforcementTests")]
    public async Task RlsPolicy_FiltersJobs_BasedOnSessionContext()
    {
        if (!CanRunAspireIntegrationTests())
        {
            Assert.Skip("Docker is not available for running SQL Server container tests");
            return;
        }

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.fencemark_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sql", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var connectionString = await app.GetConnectionStringAsync("sql", cancellationToken);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Create test organizations
        var org1Id = Guid.NewGuid().ToString();
        var org2Id = Guid.NewGuid().ToString();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO Organizations (Id, Name, CreatedAt, UpdatedAt)
                VALUES (@org1, 'Test Org 1', GETUTCDATE(), GETUTCDATE()),
                       (@org2, 'Test Org 2', GETUTCDATE(), GETUTCDATE())";
            cmd.Parameters.AddWithValue("@org1", org1Id);
            cmd.Parameters.AddWithValue("@org2", org2Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Create test jobs
        var job1Id = Guid.NewGuid().ToString();
        var job2Id = Guid.NewGuid().ToString();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO Jobs (Id, Name, CustomerName, OrganizationId, Status, CreatedAt, UpdatedAt)
                VALUES (@job1, 'Org1 Job', 'Customer 1', @org1, 0, GETUTCDATE(), GETUTCDATE()),
                       (@job2, 'Org2 Job', 'Customer 2', @org2, 0, GETUTCDATE(), GETUTCDATE())";
            cmd.Parameters.AddWithValue("@job1", job1Id);
            cmd.Parameters.AddWithValue("@job2", job2Id);
            cmd.Parameters.AddWithValue("@org1", org1Id);
            cmd.Parameters.AddWithValue("@org2", org2Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Act & Assert - Test with Org1 session context
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "EXEC sp_set_session_context @key = N'OrganizationId', @value = @orgId;";
            cmd.Parameters.AddWithValue("@orgId", org1Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Jobs";
            var count = (int)await cmd.ExecuteScalarAsync(cancellationToken);
            
            Assert.Equal(1, count);
        }

        // Test with Org2 session context
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "EXEC sp_set_session_context @key = N'OrganizationId', @value = @orgId;";
            cmd.Parameters.AddWithValue("@orgId", org2Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Jobs";
            var count = (int)await cmd.ExecuteScalarAsync(cancellationToken);
            
            Assert.Equal(1, count);
        }
    }

    [Fact(Skip = "Requires Docker and Aspire DCP - run manually with: dotnet test --filter RlsDatabaseEnforcementTests")]
    public async Task RlsPolicy_AllowsAllData_WhenSessionContextNotSet()
    {
        if (!CanRunAspireIntegrationTests())
        {
            Assert.Skip("Docker is not available for running SQL Server container tests");
            return;
        }

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.fencemark_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sql", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var connectionString = await app.GetConnectionStringAsync("sql", cancellationToken);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Create test organizations
        var org1Id = Guid.NewGuid().ToString();
        var org2Id = Guid.NewGuid().ToString();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO Organizations (Id, Name, CreatedAt, UpdatedAt)
                VALUES (@org1, 'Test Org 1', GETUTCDATE(), GETUTCDATE()),
                       (@org2, 'Test Org 2', GETUTCDATE(), GETUTCDATE())";
            cmd.Parameters.AddWithValue("@org1", org1Id);
            cmd.Parameters.AddWithValue("@org2", org2Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Create test components
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO Components (Id, Name, Category, OrganizationId, CreatedAt, UpdatedAt)
                VALUES (@comp1, 'Org1 Component', 'Post', @org1, GETUTCDATE(), GETUTCDATE()),
                       (@comp2, 'Org2 Component', 'Rail', @org2, GETUTCDATE(), GETUTCDATE())";
            cmd.Parameters.AddWithValue("@comp1", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@comp2", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@org1", org1Id);
            cmd.Parameters.AddWithValue("@org2", org2Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Act & Assert - Without setting SESSION_CONTEXT, should see all data
        // This is important for migrations and system operations
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Components";
            var count = (int)await cmd.ExecuteScalarAsync(cancellationToken);
            
            // Should see both components when no session context is set
            Assert.Equal(2, count);
        }
    }

    [Fact(Skip = "Requires Docker and Aspire DCP - run manually with: dotnet test --filter RlsDatabaseEnforcementTests")]
    public async Task RlsPolicy_AppliedToAllTenantTables()
    {
        if (!CanRunAspireIntegrationTests())
        {
            Assert.Skip("Docker is not available for running SQL Server container tests");
            return;
        }

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.fencemark_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sql", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var connectionString = await app.GetConnectionStringAsync("sql", cancellationToken);

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Act - Query for all tables with RLS predicates
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT OBJECT_NAME(target_object_id) AS TableName
            FROM sys.security_predicates
            WHERE security_policy_id = (
                SELECT security_policy_id 
                FROM sys.security_policies 
                WHERE name = 'TenantFilterPolicy'
            )
            ORDER BY TableName";

        var tables = new List<string>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        // Assert - Verify all expected tables have RLS policies
        var expectedTables = new[]
        {
            "Components",
            "DiscountRules",
            "Drawings",
            "FenceSegments",
            "FenceTypes",
            "GatePositions",
            "GateTypes",
            "Jobs",
            "Parcels",
            "PricingConfigs",
            "Quotes",
            "TaxRegions"
        }.OrderBy(t => t).ToList();

        Assert.Equal(expectedTables.Count, tables.Count);
        Assert.Equal(expectedTables, tables.OrderBy(t => t).ToList());
    }
}
