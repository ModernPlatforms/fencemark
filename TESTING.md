# Testing Guide for Fencemark

This document provides guidance on running tests for the Fencemark application, including unit tests, integration tests, and SQL Server-specific tests.

## Test Categories

### 1. Unit Tests
Fast, isolated tests that don't require external dependencies.

```bash
# Run all unit tests (default - runs in CI)
dotnet test

# Run specific test classes
dotnet test --filter-class "*OrchestrationTests"
dotnet test --filter-class "*AuthServiceTests"
dotnet test --filter-class "*PricingServiceTests"
```

### 2. Integration Tests (Aspire DCP Required)
These tests require Docker and Aspire Developer Control Plane (DCP) to be available. They are skipped by default in CI environments.

**Requirements:**
- Docker Desktop or Docker Engine installed and running
- Aspire DCP (automatically available when running via Aspire AppHost)

**Running Integration Tests Locally:**

```bash
# Run SQL Server integration tests (requires Docker)
dotnet test --filter SqlServerIntegrationTests

# Run web frontend integration tests (requires Docker and DCP)
dotnet test --filter WebTests
```

### 3. SQL Server Integration Tests

Located in `fencemark.Tests/SqlServerIntegrationTests.cs`, these tests verify:
- ✅ SQL Server container starts successfully
- ✅ API Service waits for SQL Server to be ready before starting
- ✅ Database migrations run successfully
- ✅ Health checks pass after dependencies are ready
- ✅ Connection strings are properly configured

**What These Tests Validate:**

1. **SQL Server Startup**: Verifies the SQL Server container starts and becomes healthy
2. **API Service Dependencies**: Confirms the API service waits for SQL Server (via `.WaitFor(sql)`)
3. **Database Connectivity**: Tests that the database is created and accessible
4. **Migration Success**: Validates that EF Core migrations run without errors
5. **Health Checks**: Ensures health endpoints respond correctly after startup

**Running the Tests:**

```bash
# All SQL Server integration tests
dotnet test --filter SqlServerIntegrationTests

# Specific SQL Server integration test
dotnet test --filter "ApiService_WaitForSqlServer_PreventsStartupFailures"
```

**Note**: These tests are skipped by default if Docker is not available, making them safe to run in CI environments without Docker support.

## Test Structure

### OrchestrationTests
- Configuration validation tests
- Environment-specific settings verification
- AppHost configuration checks
- **New**: Build-time validation of AppHost configuration

### SqlServerIntegrationTests
- SQL Server container lifecycle tests
- Database connectivity verification
- Migration validation
- Dependency ordering verification

### DataIsolationTests
- Multi-tenant data isolation verification
- Row-Level Security (RLS) tests
- Cross-tenant access prevention

### Performance Tests
- RLS performance benchmarks
- Query optimization validation

## Test Execution Time

| Test Suite | Typical Duration | Requirements |
|-----------|------------------|--------------|
| Unit Tests | 3-5 seconds | None |
| Orchestration Tests | < 1 second | None |
| SQL Server Integration Tests | 60-90 seconds | Docker |
| Web Integration Tests | 30-60 seconds | Docker + DCP |

## Continuous Integration

The CI pipeline automatically runs:
- ✅ All unit tests
- ✅ All orchestration tests (including build validation)
- ❌ SQL Server integration tests (skipped - requires Docker)
- ❌ Web integration tests (skipped - requires DCP)

To enable integration tests in CI, ensure the CI environment has:
1. Docker installed and running
2. Sufficient resources for SQL Server container (minimum 2GB RAM)
3. Aspire DCP available (if using hosted runners, consider GitHub's larger runners)

## Running Tests Locally with Aspire

To run the full application with SQL Server for manual testing:

```bash
# Start Aspire with SQL Server (opens Aspire Dashboard)
dotnet run --project fencemark.AppHost

# The dashboard will show:
# - SQL Server container status
# - API Service status (should wait for SQL)
# - Web Frontend status
# - Health check results
# - Logs and traces
```

## Test Best Practices

### For Developers

1. **Run unit tests before committing**:
   ```bash
   dotnet test
   ```

2. **Run integration tests when changing database or orchestration code**:
   ```bash
   dotnet test --filter SqlServerIntegrationTests
   ```

3. **Run specific test classes during development**:
   ```bash
   dotnet test --filter-class "*YourTestClass"
   ```

### For CI/CD

The build pipeline in `.github/workflows/build.yml` automatically:
- Runs all non-skipped tests
- Collects code coverage
- Reports test results

## Troubleshooting

### Integration Tests Fail with "Docker not available"

**Symptom**: SQL Server integration tests are skipped with Docker unavailability message

**Solution**:
1. Ensure Docker is installed: `docker --version`
2. Ensure Docker daemon is running: `docker ps`
3. On Windows, ensure Docker Desktop is running
4. On Linux, ensure Docker service is started: `sudo systemctl start docker`

### Integration Tests Timeout

**Symptom**: Tests fail with timeout errors

**Solution**:
1. Increase test timeout (currently 2 minutes for SQL Server tests)
2. Ensure sufficient system resources (Docker needs ~2GB RAM for SQL Server)
3. Check Docker logs: `docker logs <container-id>`
4. Review Aspire dashboard for resource status

### SQL Server Container Won't Start

**Symptom**: Integration tests fail with SQL Server health check failures

**Solution**:
1. Check Docker has sufficient resources allocated
2. Verify no port conflicts (SQL Server typically uses port 1433)
3. Review container logs in Aspire dashboard
4. Try restarting Docker

### Tests Pass Locally But Fail in CI

**Symptom**: Tests work on your machine but fail in CI pipeline

**Possible Causes**:
1. CI environment doesn't have Docker available
2. CI environment has insufficient resources
3. Time differences or timezone issues
4. Environment-specific configuration missing

**Solution**:
1. Check if integration tests are meant to be skipped in CI
2. Review CI environment specifications
3. Check CI logs for specific error messages
4. Consider using GitHub's larger runners for integration tests

## Adding New Tests

### Adding a Unit Test

```csharp
public class MyNewTests
{
    [Fact]
    public void MyTest_Should_DoSomething()
    {
        // Arrange
        var sut = new SystemUnderTest();
        
        // Act
        var result = sut.DoSomething();
        
        // Assert
        Assert.NotNull(result);
    }
}
```

### Adding an Integration Test

```csharp
[Fact(Skip = "Requires Docker - run manually", Timeout = 60000)]
public async Task MyIntegrationTest()
{
    // Arrange
    var cancellationToken = TestContext.Current.CancellationToken;
    var appHost = await DistributedApplicationTestingBuilder
        .CreateAsync<Projects.fencemark_AppHost>(cancellationToken);
    
    await using var app = await appHost.BuildAsync(cancellationToken);
    await app.StartAsync(cancellationToken);
    
    // Act & Assert
    // Your test code here
}
```

## Related Documentation

- [Aspire Orchestration Guide](ASPIRE_ORCHESTRATION.md)
- [Row-Level Security Implementation](RLS_IMPLEMENTATION.md)
- [CI/CD Pipeline Documentation](CI-CD.md)

## Summary

The Fencemark test suite provides comprehensive coverage with:
- ✅ **84 total tests** (79 unit tests, 5 integration tests)
- ✅ **Fast unit tests** that run in CI on every commit
- ✅ **Integration tests** for SQL Server dependencies (run manually)
- ✅ **Configuration validation** to catch issues early
- ✅ **Performance benchmarks** to prevent regressions

For questions or issues with tests, please file an issue in the repository.
