using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================================================
// Environment Detection and Configuration
// ============================================================================
var environmentName = builder.Environment.EnvironmentName;
var isLocal = environmentName == "Development";

Console.WriteLine($"Starting Aspire orchestration for environment: {environmentName}");

// ============================================================================
// Shared SQL Server (Aspire resource)
// ============================================================================
// When running under Aspire locally, this SQL resource will be started
// in a container and its connection string will be injected as
// ConnectionStrings:DefaultConnection into referencing projects.
var sql = builder.AddSqlServer("sql", port: 1433)
    .WithLifetime(ContainerLifetime.Persistent);

var sqldb=sql.AddDatabase("fencemark");
 

// ============================================================================
// API Service Configuration
// ============================================================================
var apiService = builder.AddProject<Projects.fencemark_ApiService>("apiservice")
    .WithReference(sqldb)
    .WithReplicas(GetMinReplicas(environmentName, "ApiService"));

// Apply environment-specific resource limits for non-local environments
if (!isLocal)
{
    apiService = ApplyResourceLimits(apiService, environmentName, "ApiService");
}

// ============================================================================
// Web Frontend Configuration
// ============================================================================
var webFrontend = builder.AddProject<Projects.fencemark_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReplicas(GetMinReplicas(environmentName, "WebFrontend"));

// Apply environment-specific resource limits for non-local environments
if (!isLocal)
{
    webFrontend = ApplyResourceLimits(webFrontend, environmentName, "WebFrontend");
}

var host = builder.Build();

Console.WriteLine($"Aspire orchestration configured for {environmentName} environment");
Console.WriteLine($"API Service: {GetMinReplicas(environmentName, "ApiService")} replica(s)");
Console.WriteLine($"Web Frontend: {GetMinReplicas(environmentName, "WebFrontend")} replica(s)");

host.Run();

// ============================================================================
// Helper Methods for Environment-Specific Configuration
// ============================================================================

static int GetMinReplicas(string environment, string service)
{
    return environment switch
    {
        "Development" => 1,
        "Test" => 1,
        "Staging" => 1,
        "Production" => 2,
        _ => 1
    };
}

static IResourceBuilder<ProjectResource> ApplyResourceLimits(
    IResourceBuilder<ProjectResource> resource,
    string environment,
    string serviceName)
{
    // Resource limits are primarily enforced in Azure Container Apps via Bicep
    // This method is a placeholder for any Aspire-specific resource configuration
    // that might be needed for local testing or other orchestration scenarios
    return resource;
}
