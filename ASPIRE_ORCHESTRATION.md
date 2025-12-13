# Aspire Orchestration Guide

## Overview

This guide provides comprehensive documentation for using .NET Aspire to orchestrate the Fencemark application across different environments (local, test, staging, and production). Aspire manages service discovery, health checks, observability, and resource allocation for distributed applications.

## Table of Contents

- [Architecture](#architecture)
- [Environment Overview](#environment-overview)
- [Local Development Orchestration](#local-development-orchestration)
- [Test Environment Orchestration](#test-environment-orchestration)
- [Staging Environment Orchestration](#staging-environment-orchestration)
- [Production Environment Orchestration](#production-environment-orchestration)
- [Environment Transitions](#environment-transitions)
- [Configuration Management](#configuration-management)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Architecture

The Fencemark application uses .NET Aspire for orchestration, which provides:

- **Service Discovery**: Automatic service-to-service communication
- **Health Checks**: Built-in health monitoring for all services
- **OpenTelemetry**: Distributed tracing, metrics, and logging
- **Resilience**: Automatic retry policies and circuit breakers
- **Resource Management**: Environment-specific resource allocation

```
┌─────────────────────────────────────────────────────────────┐
│                    Aspire AppHost                           │
│                                                             │
│  ┌─────────────────────┐         ┌─────────────────────┐   │
│  │   Web Frontend      │────────▶│   API Service       │   │
│  │  (Blazor Server)    │         │  (Minimal API)      │   │
│  │                     │         │                     │   │
│  │ • Health Checks     │         │ • Health Checks     │   │
│  │ • Service Discovery │         │ • Service Discovery │   │
│  │ • OpenTelemetry     │         │ • OpenTelemetry     │   │
│  │ • Resilience        │         │ • Resilience        │   │
│  └─────────────────────┘         └─────────────────────┘   │
│            │                              │                 │
│            └──────────────┬───────────────┘                 │
│                           ▼                                 │
│                  Service Defaults                           │
│         (Shared Configuration & Extensions)                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Environment Overview

Fencemark supports four distinct environments, each with specific orchestration characteristics:

| Environment | Purpose | Auto-Deploy | Replicas | Resources | Database |
|-------------|---------|-------------|----------|-----------|----------|
| **Local (Development)** | Local development and debugging | No | 1 | Minimal | SQLite |
| **Test** | Automated testing and CI/CD validation | Yes (on PR) | 1-2 | Minimal | SQLite |
| **Staging** | Pre-production validation and UAT | Manual | 1-3 | Moderate | SQL Server |
| **Production** | Live user-facing application | Manual | 2-10 | Full | SQL Server |

### Environment Configuration Files

Each environment has its own `appsettings.{Environment}.json` file in the `fencemark.AppHost` project:

- `appsettings.json` - Base configuration (shared across all environments)
- `appsettings.Development.json` - Local development settings
- `appsettings.Test.json` - Test environment settings
- `appsettings.Staging.json` - Staging environment settings
- `appsettings.Production.json` - Production environment settings

## Local Development Orchestration

### Purpose

The local development environment is optimized for:
- Fast iteration and debugging
- Single-developer workflows
- Minimal resource consumption
- Immediate feedback on code changes

### Starting the Application Locally

```bash
# Navigate to the repository root
cd fencemark

# Run the AppHost to start all services
dotnet run --project fencemark.AppHost
```

### What Happens

1. **Aspire Dashboard Opens**: A browser window opens showing the Aspire dashboard at `http://localhost:15888`
2. **Services Start**: Both API Service and Web Frontend start automatically
3. **Health Checks Run**: Each service reports its health status to the dashboard
4. **Service Discovery**: The Web Frontend automatically discovers the API Service endpoint
5. **SQLite Database**: A local SQLite database is created automatically (`fencemark.db`)

### Aspire Dashboard Features

The Aspire dashboard provides real-time insights:

- **Resources**: View all running services and their status
- **Console Logs**: Stream logs from each service
- **Structured Logs**: View structured log entries with filtering
- **Traces**: Distributed tracing across service boundaries
- **Metrics**: Performance metrics for each service
- **Endpoints**: Quick access to service URLs

### Local Configuration

| Setting | Value |
|---------|-------|
| **Environment** | Development |
| **API Service Replicas** | 1 |
| **Web Frontend Replicas** | 1 |
| **Database** | SQLite (local file) |
| **OpenTelemetry** | Enabled (Aspire Dashboard) |
| **Service Discovery** | Local DNS |

### Local Development Tips

1. **Hot Reload**: Changes to Blazor components reload automatically
2. **API Changes**: Restart the AppHost to pick up API changes
3. **Database Reset**: Delete `fencemark.db` to reset the database
4. **Port Conflicts**: Aspire automatically assigns available ports
5. **Debug Multiple Services**: Attach debugger to individual service processes

## Test Environment Orchestration

### Purpose

The test environment is designed for:
- Automated testing in CI/CD pipelines
- Integration testing with realistic service interactions
- Validation before merging code changes
- Minimal resource consumption for cost efficiency

### Running Tests with Aspire

Tests use Aspire's `DistributedApplicationTestingBuilder` for integration testing:

```bash
# Run all tests
dotnet test

# Run only integration tests
dotnet test --filter "FullyQualifiedName~WebTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Test Orchestration Flow

1. **Test Host Starts**: Aspire creates a test host with in-memory service discovery
2. **Services Launch**: API Service and Web Frontend start in test mode
3. **SQLite Database**: A temporary SQLite database is created per test run
4. **Tests Execute**: Integration tests run against the orchestrated services
5. **Cleanup**: All resources are disposed after tests complete

### Test Environment Configuration

| Setting | Value |
|---------|-------|
| **Environment** | Test |
| **API Service Replicas** | 1 |
| **Web Frontend Replicas** | 1 |
| **Database** | SQLite (in-memory or temp file) |
| **OpenTelemetry** | Enabled (test output) |
| **Service Discovery** | In-memory |

### CI/CD Integration

The test environment is automatically used by GitHub Actions:

```yaml
# .github/workflows/build.yml
- name: Run tests
  run: dotnet test --no-build --configuration Release
```

Tests validate:
- Service health and startup
- API endpoints and contracts
- Service-to-service communication
- Authentication and authorization
- Database migrations

## Staging Environment Orchestration

### Purpose

The staging environment mirrors production and is used for:
- User Acceptance Testing (UAT)
- Performance testing under realistic load
- Final validation before production deployment
- Training and demonstration

### Deploying to Staging

Staging deployments are **manual** and require approval:

```bash
# Option 1: Via GitHub Actions (Recommended)
# Go to Actions → Deploy to Azure → Run workflow → Select "staging"

# Option 2: Via Azure CLI
az deployment sub create \
  --location australiaeast \
  --template-file ./infra/main.bicep \
  --parameters ./infra/staging.bicepparam \
  --name staging-deployment
```

### Staging Environment Configuration

| Setting | Value |
|---------|-------|
| **Environment** | Staging |
| **API Service Replicas** | 1-3 (auto-scales) |
| **Web Frontend Replicas** | 1-3 (auto-scales) |
| **CPU per Container** | 0.5 cores |
| **Memory per Container** | 1 GiB |
| **Database** | Azure SQL Database |
| **OpenTelemetry** | Azure Application Insights |
| **Service Discovery** | Azure Container Apps |

### Staging Orchestration Features

1. **Auto-Scaling**: Scales from 1 to 3 replicas based on load
2. **Health Probes**: Azure Container Apps monitors `/health` endpoint
3. **Zero-Downtime Deployments**: Rolling updates with health checks
4. **Configuration Management**: Environment variables from Azure App Configuration
5. **Secrets Management**: Secrets retrieved from Azure Key Vault

### Accessing Staging

After deployment, the staging URL is available in:
- GitHub Actions deployment summary
- Azure Portal (Container Apps → Overview)
- Azure CLI: `az containerapp show --name ca-webfrontend-staging --resource-group rg-fencemark-staging --query properties.configuration.ingress.fqdn`

### Staging Best Practices

1. **Test Thoroughly**: Run end-to-end tests before promoting to production
2. **Monitor Metrics**: Use Application Insights to validate performance
3. **Check Logs**: Review logs for errors or warnings
4. **Validate Configuration**: Ensure all environment variables are correct
5. **Test Rollback**: Verify rollback procedures work correctly

## Production Environment Orchestration

### Purpose

The production environment serves live users and requires:
- High availability (99.9% uptime SLA)
- Auto-scaling to handle traffic spikes
- Comprehensive monitoring and alerting
- Disaster recovery capabilities

### Deploying to Production

Production deployments are **manual** and require approval from designated reviewers:

```bash
# Option 1: Via GitHub Actions (Recommended)
# Go to Actions → Deploy to Azure → Run workflow → Select "prod"
# Approval required from production reviewers

# Option 2: Via Azure CLI
az deployment sub create \
  --location australiaeast \
  --template-file ./infra/main.bicep \
  --parameters ./infra/prod.bicepparam \
  --name prod-deployment
```

### Production Environment Configuration

| Setting | Value |
|---------|-------|
| **Environment** | Production |
| **API Service Replicas** | 2-10 (auto-scales) |
| **Web Frontend Replicas** | 2-10 (auto-scales) |
| **CPU per Container** | 1 core |
| **Memory per Container** | 2 GiB |
| **Database** | Azure SQL Database (HA) |
| **OpenTelemetry** | Azure Application Insights |
| **Service Discovery** | Azure Container Apps |

### Production Orchestration Features

1. **High Availability**: Minimum 2 replicas always running
2. **Auto-Scaling**: Scales from 2 to 10 replicas based on CPU, memory, and HTTP queue length
3. **Health Probes**: 
   - **Liveness**: `/alive` endpoint (ensures app is responsive)
   - **Readiness**: `/health` endpoint (ensures app is ready for traffic)
4. **Rolling Updates**: Zero-downtime deployments with health validation
5. **Automatic Failover**: Unhealthy replicas are automatically replaced
6. **Traffic Management**: Load balancing across healthy replicas

### Production Monitoring

Production services are monitored via:

1. **Application Insights**: Distributed tracing, metrics, and logs
2. **Azure Monitor**: Infrastructure metrics and alerts
3. **Health Checks**: Continuous health validation
4. **Custom Metrics**: Business-specific metrics via OpenTelemetry

### Production Alerts

Configure alerts for:
- High error rates (> 5% of requests)
- Slow response times (> 1 second p95)
- Failed health checks
- Low replica count (< 2)
- High CPU or memory usage (> 80%)
- Database connection failures

## Environment Transitions

### Local → Test

**Trigger**: Automated on every push/PR

**Process**:
1. Developer commits code changes
2. GitHub Actions build workflow starts
3. Tests run using Aspire test orchestration
4. Results reported in PR or commit status

**Validation**:
- All tests pass
- Code builds successfully
- No security vulnerabilities detected

### Test → Staging

**Trigger**: Manual deployment after successful tests

**Process**:
1. Navigate to GitHub Actions → Deploy to Azure
2. Select "staging" environment
3. Click "Run workflow"
4. Wait for deployment to complete
5. Verify deployment in Azure Portal

**Validation**:
- Health checks pass
- Integration tests pass in staging
- UAT sign-off from stakeholders
- Performance benchmarks meet targets

### Staging → Production

**Trigger**: Manual deployment with approval

**Process**:
1. Navigate to GitHub Actions → Deploy to Azure
2. Select "prod" environment
3. Click "Run workflow"
4. Wait for approval from designated reviewers
5. Monitor deployment progress
6. Verify deployment in Azure Portal

**Validation**:
- All staging validations passed
- Change management approval received
- Rollback plan documented
- Monitoring and alerts configured
- On-call team notified

### Rollback Procedures

If issues are detected after deployment:

```bash
# Option 1: Rollback via Azure CLI
az containerapp revision list \
  --name ca-webfrontend-<resourceToken> \
  --resource-group rg-fencemark-<environment>

az containerapp revision activate \
  --name ca-webfrontend-<resourceToken> \
  --resource-group rg-fencemark-<environment> \
  --revision <previous-revision-name>

# Option 2: Redeploy previous commit
# Go to GitHub Actions → Deploy to Azure → Run workflow with previous commit
```

## Configuration Management

### Environment Variables

Aspire injects environment variables into each service based on the environment:

**Development/Test**:
```bash
ASPNETCORE_ENVIRONMENT=Development
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
ConnectionStrings__DefaultConnection=Data Source=fencemark.db
```

**Staging/Production**:
```bash
ASPNETCORE_ENVIRONMENT=Production
OTEL_EXPORTER_OTLP_ENDPOINT=<Application Insights endpoint>
ConnectionStrings__DefaultConnection=<Azure SQL connection string>
APPLICATIONINSIGHTS_CONNECTION_STRING=<AppInsights connection>
EntraExternalId__TenantId=<tenant-id>
EntraExternalId__ClientId=<client-id>
KeyVault__Url=<keyvault-url>
```

### Secrets Management

**Local**: User Secrets (`dotnet user-secrets`)
```bash
dotnet user-secrets set "ApiKey" "your-secret-key" --project fencemark.AppHost
```

**Azure**: Azure Key Vault integration
- Secrets are stored in Azure Key Vault
- Container Apps retrieve secrets using Managed Identity
- No secrets in code or configuration files

### Configuration Hierarchy

Configuration is loaded in order (later sources override earlier):

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific)
3. User Secrets (local development only)
4. Environment Variables (all environments)
5. Azure App Configuration (staging/production)
6. Command-line arguments (overrides)

## Best Practices

### 1. Always Use Aspire for Local Development

✅ **Do**: Run the full application via `dotnet run --project fencemark.AppHost`

❌ **Don't**: Run individual projects directly unless debugging specific issues

**Why**: Aspire ensures service discovery, health checks, and observability work correctly.

### 2. Test Environment-Specific Configuration Locally

Test different environments locally:

```bash
# Test with staging configuration
dotnet run --project fencemark.AppHost --environment Staging

# Test with production configuration (without Azure resources)
dotnet run --project fencemark.AppHost --environment Production
```

### 3. Monitor Health Checks

Always configure health checks for your services:

```csharp
// In Program.cs
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});
```

### 4. Use Service Discovery

Let Aspire handle service-to-service communication:

```csharp
// ✅ Good: Use service name from Aspire
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://apiservice");
});

// ❌ Bad: Hardcode URLs
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
});
```

### 5. Implement Retry Policies

Service Defaults includes standard resilience handlers:

```csharp
// Automatically added by Service Defaults
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
});
```

### 6. Validate Deployments

After deploying, always verify:

```bash
# Check health endpoint
curl https://your-app-url.azurecontainerapps.io/health

# Check logs
az containerapp logs show \
  --name ca-webfrontend-<resourceToken> \
  --resource-group rg-fencemark-<environment> \
  --follow

# Check metrics in Aspire Dashboard (local) or Application Insights (Azure)
```

### 7. Use Environment-Specific Scaling

Configure replicas based on environment needs:

| Environment | Min Replicas | Max Replicas | Rationale |
|-------------|--------------|--------------|-----------|
| Local | 1 | 1 | Single developer, no scaling needed |
| Test | 1 | 2 | CI/CD testing, minimal resources |
| Staging | 1 | 3 | UAT and performance testing |
| Production | 2 | 10 | High availability and traffic handling |

### 8. Document Environment-Specific Behavior

Document any behavior that differs between environments:

```csharp
// Local: Uses SQLite for fast iteration
// Staging/Production: Uses Azure SQL for reliability
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
}
```

### 9. Leverage OpenTelemetry

Aspire automatically configures OpenTelemetry for observability:

- **Traces**: Track requests across service boundaries
- **Metrics**: Monitor performance and resource usage
- **Logs**: Structured logging with correlation IDs

All telemetry is visible in:
- **Local**: Aspire Dashboard
- **Azure**: Application Insights

### 10. Plan for Failure

Design services to handle failures gracefully:

- **Circuit Breakers**: Automatically enabled via resilience handlers
- **Retry Policies**: Exponential backoff for transient failures
- **Health Checks**: Report service health accurately
- **Graceful Degradation**: Continue operating with reduced functionality

## Troubleshooting

### Issue: Service Won't Start Locally

**Symptoms**: Service fails to start, or Aspire Dashboard shows service in error state

**Solutions**:

1. **Check Port Conflicts**:
   ```bash
   # View port assignments in Aspire Dashboard
   # Aspire automatically assigns available ports
   ```

2. **Check Logs**:
   ```bash
   # View service logs in Aspire Dashboard → Console Logs tab
   ```

3. **Verify Dependencies**:
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Clear Build Artifacts**:
   ```bash
   dotnet clean
   dotnet build
   ```

### Issue: Service Discovery Not Working

**Symptoms**: Web Frontend can't reach API Service

**Solutions**:

1. **Verify Service Names**: Ensure service names match in AppHost and HttpClient configuration
   ```csharp
   // In AppHost.cs
   var apiService = builder.AddProject<Projects.fencemark_ApiService>("apiservice");
   
   // In Web Frontend Program.cs
   builder.Services.AddHttpClient<ApiClient>(client =>
   {
       client.BaseAddress = new Uri("http://apiservice"); // Must match!
   });
   ```

2. **Check Service Defaults**: Ensure both services reference `fencemark.ServiceDefaults`

3. **Verify Service Discovery is Enabled**:
   ```csharp
   builder.AddServiceDefaults(); // Required in both services
   ```

### Issue: Health Checks Failing

**Symptoms**: Health check endpoint returns unhealthy status

**Solutions**:

1. **Check Database Connectivity**:
   ```bash
   # Verify database file exists (SQLite) or connection string is valid (SQL Server)
   ```

2. **Review Health Check Configuration**:
   ```csharp
   builder.Services.AddHealthChecks()
       .AddDbContextCheck<AppDbContext>()
       .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
   ```

3. **Check Dependencies**: Ensure all required services are running

### Issue: Environment Configuration Not Loading

**Symptoms**: Application uses wrong configuration for environment

**Solutions**:

1. **Set Environment Variable**:
   ```bash
   # Windows
   $env:ASPNETCORE_ENVIRONMENT="Staging"
   dotnet run --project fencemark.AppHost
   
   # Linux/macOS
   ASPNETCORE_ENVIRONMENT=Staging dotnet run --project fencemark.AppHost
   ```

2. **Use Command-Line Argument**:
   ```bash
   dotnet run --project fencemark.AppHost --environment Staging
   ```

3. **Verify appsettings Files**: Check that `appsettings.{Environment}.json` exists

### Issue: Deployment Fails in Azure

**Symptoms**: Container Apps deployment fails or app doesn't start

**Solutions**:

1. **Check Deployment Logs**:
   ```bash
   az deployment sub show \
     --name main \
     --query properties.error
   ```

2. **Verify Container App Logs**:
   ```bash
   az containerapp logs show \
     --name ca-webfrontend-<resourceToken> \
     --resource-group rg-fencemark-<environment> \
     --follow
   ```

3. **Check Health Probes**: Ensure `/health` and `/alive` endpoints work:
   ```bash
   curl https://your-app.azurecontainerapps.io/health
   ```

4. **Validate Environment Variables**:
   ```bash
   az containerapp show \
     --name ca-webfrontend-<resourceToken> \
     --resource-group rg-fencemark-<environment> \
     --query properties.template.containers[0].env
   ```

5. **Check Managed Identity Permissions**: Verify Key Vault access

### Issue: Slow Performance in Staging/Production

**Symptoms**: Application responds slowly or times out

**Solutions**:

1. **Check Application Insights**: Review performance metrics and slow requests

2. **Review Resource Limits**:
   ```bash
   az containerapp show \
     --name ca-webfrontend-<resourceToken> \
     --resource-group rg-fencemark-<environment> \
     --query properties.template.containers[0].resources
   ```

3. **Check Replica Count**:
   ```bash
   az containerapp replica list \
     --name ca-webfrontend-<resourceToken> \
     --resource-group rg-fencemark-<environment>
   ```

4. **Enable Auto-Scaling**: Verify scaling rules are configured correctly

### Issue: Can't Connect to Database

**Symptoms**: Database connection errors in logs

**Solutions**:

1. **Local (SQLite)**:
   - Verify file path and permissions
   - Check that database file is not locked by another process

2. **Azure (SQL Server)**:
   - Verify connection string in environment variables
   - Check firewall rules allow Container Apps
   - Verify Managed Identity has database access

### Getting Help

If you can't resolve an issue:

1. **Check Documentation**:
   - [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
   - [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)

2. **Review Logs**:
   - Aspire Dashboard (local)
   - Application Insights (Azure)
   - Azure Container Apps logs

3. **GitHub Issues**: File an issue in the repository with:
   - Environment (local/test/staging/production)
   - Error messages from logs
   - Steps to reproduce
   - Expected vs. actual behavior

4. **Azure Support**: Contact Azure support for infrastructure issues

## Summary

.NET Aspire provides powerful orchestration capabilities for managing distributed applications across multiple environments. By following this guide, teams can:

- ✅ Reliably provision and configure each environment
- ✅ Automate transitions between environments
- ✅ Minimize manual configuration and reduce errors
- ✅ Monitor and troubleshoot issues effectively
- ✅ Scale applications based on environment needs

For more information, see:
- [CI/CD Pipeline Documentation](CI-CD.md)
- [Manual Deployment Guide](DEPLOYMENT.md)
- [Infrastructure Details](infra/README.md)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
