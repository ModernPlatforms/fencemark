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

### 4. End-to-End (E2E) Playwright Tests

Located in `fencemark.Tests/E2E/`, these tests verify the complete user workflow using browser automation:

**Test Suites:**
- **AuthenticationFlowE2ETests** - Login, logout, and session management with persistent test user
- **ComponentFlowE2ETests** - Component CRUD operations via UI and API
- **ComprehensiveJobFlowE2ETests** - Job management including create, update, delete, and view
- **AllEndpointsE2ETests** - Comprehensive tests for all major endpoints with automatic cleanup
- **JobFlowE2ETests** - Legacy job workflow tests
- **QuoteFlowE2ETests** - Quote generation and management
- **BillingFlowE2ETests** - Billing and pricing configuration

**Key Features:**
- ✅ Uses persistent test user (no registration/deletion during tests)
- ✅ Cookie-based authentication with session management
- ✅ Automatic cleanup of test data (components, jobs, fences, gates)
- ✅ Screenshot capture for debugging
- ✅ Video recording of test runs
- ✅ Environment variable configuration
- ✅ Headless and headed mode support

**Running E2E Tests:**

```bash
# Set REQUIRED environment variables
export TEST_BASE_URL="https://your-dev-environment.azurewebsites.net"
export TEST_USER_EMAIL="testuser@yourdomain.com"  # REQUIRED: Persistent test user
export TEST_USER_PASSWORD="YourSecurePassword"     # REQUIRED: From Azure Key Vault
export TEST_HEADLESS="false"  # Optional: Set to false to see browser

# Run all E2E tests (requires running application)
dotnet test --filter "FullyQualifiedName~E2ETests"

# Run specific test suite
dotnet test --filter "AuthenticationFlowE2ETests"
dotnet test --filter "ComponentFlowE2ETests"
dotnet test --filter "ComprehensiveJobFlowE2ETests"
dotnet test --filter "AllEndpointsE2ETests"

# Run single test
dotnet test --filter "CanCreateAndDeleteComponent"
```

**Prerequisites:**
- Application must be running (via `dotnet run --project fencemark.AppHost` or deployed to Azure)
- Playwright browsers installed (`playwright install chromium`)
- **Persistent test user created in the environment** with email and password
- Test user credentials stored in Azure Key Vault and loaded via environment variables

**Test User Management:**
- Tests use a single persistent test user (set via `TEST_USER_EMAIL` environment variable)
- Tests login at the start and reuse the same session
- Tests clean up test data (components, jobs, fences, gates) after completion
- Test user account is NOT deleted - it's reused across test runs
- Test user credentials should be stored in Azure Key Vault

**Setting Up Test User in Dev:**
1. Create a test user account in your dev environment (register via UI or API)
2. Store credentials in Azure Key Vault:
   ```bash
   az keyvault secret set --vault-name your-dev-keyvault --name test-user-email --value "testuser@yourdomain.com"
   az keyvault secret set --vault-name your-dev-keyvault --name test-user-password --value "YourSecurePassword"
   ```
3. Configure environment variables to read from Key Vault or set them directly for local testing

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

## End-to-End (E2E) Testing with Playwright

### Overview

Fencemark includes Playwright-based E2E tests for comprehensive workflow testing across the entire application stack.

**Location:** `fencemark.Tests/E2E/`

**Test Suites:**
- `JobFlowE2ETests` - Tests for drawing/fence management workflows
- `QuoteFlowE2ETests` - Tests for quote generation and management
- `BillingFlowE2ETests` - Tests for pricing and billing workflows

### Running E2E Tests Locally

E2E tests require the application to be running. They are skipped by default in CI/CD.

```bash
# 1. Install Playwright browsers (first time only)
pwsh bin/Debug/net10.0/playwright.ps1 install

# 2. Start the application
dotnet run --project fencemark.AppHost

# 3. In another terminal, run E2E tests
export TEST_BASE_URL=http://localhost:5000
dotnet test --filter "JobFlowE2ETests|QuoteFlowE2ETests|BillingFlowE2ETests"
```

### E2E Test Configuration

Tests can be configured via environment variables:

- `TEST_BASE_URL` - Base URL of the application (default: `http://localhost:5000`)
- Set in `PlaywrightTestBase.cs`

### E2E Test Artifacts

When tests run, they generate:
- **Screenshots** - Saved to `screenshots/` directory
- **Videos** - Saved to `videos/` directory (on failure)

These are excluded from git via `.gitignore`.

### Adding New E2E Tests

1. Create a new test class inheriting from `PlaywrightTestBase`
2. Use Playwright's API to interact with the UI
3. Add `[Fact(Skip = "E2E tests require running application")]` attribute
4. Use `data-testid` attributes in UI for reliable selectors

Example:
```csharp
public class MyE2ETests : PlaywrightTestBase
{
    [Fact(Skip = "E2E tests require running application")]
    public async Task CanPerformAction()
    {
        await SetupAsync();
        try
        {
            await NavigateToAsync("/my-page");
            await Page!.ClickAsync("[data-testid='my-button']");
            // Assert...
        }
        finally
        {
            await TeardownAsync();
        }
    }
}
```

## Nightly Tests

Comprehensive regression tests run automatically every night at 2 AM UTC.

**Workflow:** `.github/workflows/nightly-tests.yml`

**What it tests:**
- All unit tests
- Integration tests (where environment supports)
- Smoke tests against deployed environments (dev/staging/prod)
- Health checks on all services

**Failure handling:**
- Automatic GitHub issue creation on failure
- Tagged with `test-failure` and `automated` labels
- Notifications sent to configured channels

**Manual trigger:**
```bash
# Via GitHub Actions UI: Actions → Nightly Tests → Run workflow
# Select environment to test: dev, staging, or prod
```

## Related Documentation

- [Aspire Orchestration Guide](ASPIRE_ORCHESTRATION.md)
- [Row-Level Security Implementation](RLS_IMPLEMENTATION.md)
- [CI/CD Pipeline Documentation](CI-CD.md)
- [Rollback Procedures](ROLLBACK.md)

## Summary

The Fencemark test suite provides comprehensive coverage with:
- ✅ **105 total tests** (100 passing unit tests, 5 skipped integration tests)
- ✅ **Fast unit tests** that run in CI on every commit
- ✅ **Integration tests** for SQL Server dependencies (run manually)
- ✅ **E2E tests** for critical user workflows (12 skeleton tests)
- ✅ **Configuration validation** to catch issues early
- ✅ **Performance benchmarks** to prevent regressions
- ✅ **Nightly regression testing** with automatic issue creation
- ✅ **Smoke tests** against deployed environments

For questions or issues with tests, please file an issue in the repository.
