# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Run the full application (starts all services via .NET Aspire)
dotnet run --project fencemark.AppHost

# Build the solution
dotnet build

# Run all tests
dotnet test

# Run unit tests only (faster)
dotnet test --filter "FullyQualifiedName~AuthServiceTests|FullyQualifiedName~OrganizationServiceTests"

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Run E2E tests (requires AppHost running)
dotnet test fencemark.Tests --filter "FullyQualifiedName~AllEndpointsE2ETests"

# Publish Blazor WASM client for local nginx serving
dotnet publish fencemark.Client -c Release
```

### E2E Test Environment Variables
```powershell
$env:TEST_USER_EMAIL="test@fencemark.local"
$env:TEST_USER_PASSWORD="TestPassword123!"
$env:TEST_HEADLESS="false"  # Set to see browser
```

### First-time Setup
Install Playwright browsers (required for E2E tests):
```bash
pwsh fencemark.Tests/bin/Debug/net10.0/playwright.ps1 install
```

## Architecture Overview

Fencemark is a **multi-tenant B2B application** for fence contractor job estimation and quoting, built with .NET Aspire orchestration.

### Project Structure

| Project | Description |
|---------|-------------|
| `fencemark.AppHost` | Aspire orchestration - starts SQL Server container, API service, and nginx for WASM client |
| `fencemark.Client` | Blazor WebAssembly frontend (MudBlazor UI), authenticates via Azure Entra External ID |
| `fencemark.ApiService` | ASP.NET Core Minimal API with JWT auth, EF Core, SQL Server |
| `fencemark.ServiceDefaults` | Shared Aspire configuration (OpenTelemetry, health checks, resilience) |
| `fencemark.Tests` | Unit tests, integration tests (Aspire testing), and Playwright E2E tests |

### Key Architecture Patterns

**Multi-Tenancy via SQL Server RLS:**
- `TenantConnectionInterceptor` sets `SESSION_CONTEXT` with OrganizationId before each query
- All tenant-scoped entities implement `IOrganizationScoped` interface
- RLS policies in SQL Server enforce data isolation at database level

**Authentication Flow:**
1. Blazor WASM authenticates via MSAL.js to Azure Entra External ID (CIAM)
2. JWT tokens are sent to API in Authorization header
3. `OnTokenValidated` event auto-creates users and links to organizations
4. Custom claims (`ApplicationUserId`, `OrganizationId`) are added to principal

**Feature-Based Organization:**
- API endpoints organized under `Features/` (Auth, Jobs, Fences, Gates, Components, etc.)
- Each feature has: `*Endpoints.cs` (minimal API routes), models/DTOs
- Services in `Services/` folder (AuthService, OrganizationService, PricingService)

### Data Model Highlights

- **Organization** → owns all tenant data
- **ApplicationUser** → extends IdentityUser with ExternalId (Azure AD oid)
- **OrganizationMember** → links users to organizations with roles
- **Job** → represents a fencing project with line items
- **FenceType/GateType** → product catalog with component compositions
- **Quote** → generated from Job with pricing config, BOM, versioning

### Local Development

The AppHost starts:
1. SQL Server container (port 1433, persistent)
2. API Service with auto-migration
3. nginx container serving published WASM client (HTTP: 5173, HTTPS: 7173)

WASM client connects to API at `https://localhost:62010` (or Aspire-assigned port).

### Infrastructure (Azure)

- **Production**: Blazor WASM on Azure Static Web Apps, API on Container Apps, Azure SQL
- **IaC**: Bicep templates in `infra/` using Azure Verified Modules
- **CI/CD**: GitHub Actions workflows for build, test, and deployment

### Key Configuration Files

- `fencemark.Client/wwwroot/appsettings.json` - WASM client config (gitignored, created at build)
- `fencemark.ApiService/appsettings.json` - API config including AzureAd settings
- `fencemark.AppHost/nginx.conf` - nginx config for local WASM serving
