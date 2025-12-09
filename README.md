# Fencemark

A distributed web application built with [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview), featuring a Blazor Server frontend and an ASP.NET Core API backend.

## Overview

Fencemark is a modern cloud-native application that demonstrates the power of .NET Aspire for building distributed systems. It consists of:

- **Web Frontend** - A Blazor Server application with interactive components
- **API Service** - An ASP.NET Core minimal API with OpenAPI documentation
- **Service Defaults** - Shared service configuration for observability and resilience
- **App Host** - The orchestration layer that manages the distributed application

```
┌─────────────────────────────────────────────────────────┐
│                      App Host                           │
│  ┌─────────────────────┐    ┌─────────────────────┐    │
│  │    Web Frontend     │───▶│     API Service     │    │
│  │   (Blazor Server)   │    │   (Minimal API)     │    │
│  └─────────────────────┘    └─────────────────────┘    │
│              │                        │                 │
│              └────────┬───────────────┘                 │
│                       ▼                                 │
│              ┌─────────────────┐                        │
│              │ Service Defaults│                        │
│              └─────────────────┘                        │
└─────────────────────────────────────────────────────────┘
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [.NET Aspire workload](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)

To install the Aspire workload:

```bash
dotnet workload install aspire
```

## Getting Started

### Clone the repository

```bash
git clone https://github.com/your-username/fencemark.git
cd fencemark
```

### Run the application

Start the application using the App Host:

```bash
dotnet run --project fencemark.AppHost
```

The Aspire dashboard will open in your browser, providing:
- Real-time logs from all services
- Distributed tracing
- Health check status
- Service endpoints

The database migrations will run automatically on application startup.

### First-time Setup

1. Navigate to the web frontend URL (shown in Aspire dashboard)
2. Click "Register" to create your first account
3. Fill in:
   - Your email address
   - A secure password (min. 8 chars, uppercase, lowercase, number)
   - Your organization name
4. You'll be created as the Owner of your new organization
5. After registration, you can invite team members from the Organization Dashboard

> [!TIP]
> The web frontend is available at the URL shown in the Aspire dashboard under the `webfrontend` resource.

## Project Structure

| Project | Description |
|---------|-------------|
| `fencemark.AppHost` | Aspire orchestration host that coordinates all services |
| `fencemark.Web` | Blazor Server frontend with interactive components |
| `fencemark.ApiService` | ASP.NET Core minimal API backend |
| `fencemark.ServiceDefaults` | Shared configuration for OpenTelemetry, health checks, and resilience |
| `fencemark.Tests` | Integration tests using Aspire's distributed testing framework |

## Running Tests

Run all tests:

```bash
dotnet test
```

Run only unit tests (faster):

```bash
dotnet test --filter "FullyQualifiedName~AuthServiceTests|FullyQualifiedName~OrganizationServiceTests"
```

The test suite includes:
- **Unit Tests** - Comprehensive tests for authentication and organization management services
- **Integration Tests** - Full application testing using Aspire's `DistributedApplicationTestingBuilder`

### Test Coverage

The test suite covers all acceptance criteria:
- ✅ Automatic organization creation when a new user signs up
- ✅ User is set as initial owner/admin of their organization
- ✅ Guest status for unverified users
- ✅ Email verification removes guest status
- ✅ Admin can invite additional users with role assignment
- ✅ Role-based access control (Owner, Admin, Member, Billing, ReadOnly)
- ✅ Complete data segmentation between organizations
- ✅ Success and error state handling

## Features

- **B2B User Onboarding** - Complete organizational user management system
  - Automatic organization creation upon user registration
  - Owner/Admin role assignment for organizational control
  - Guest status for unverified users
  - Email verification workflow
  - Multi-role support (Owner, Admin, Member, Billing, ReadOnly)
  - User invitation system with token-based acceptance
  - Complete data isolation between organizations
- **Service Discovery** - Automatic service discovery between components using Aspire
- **Health Checks** - Built-in health endpoints at `/health` for each service
- **OpenAPI** - API documentation available in development mode
- **Output Caching** - Response caching in the web frontend
- **Resilience** - HTTP client resilience patterns for service-to-service communication
- **Authentication & Authorization** - Azure Entra External ID (CIAM) for customer authentication with ASP.NET Core Identity for API access and role-based authorization
  - OpenID Connect authentication
  - Certificate-based authentication from Azure Key Vault
  - Multi-role support (Owner, Admin, Member, Billing, ReadOnly)
  - Secure token management
- **Database** - Entity Framework Core with SQLite (development) or SQL Server/PostgreSQL (production)

## Deployment

The application can be deployed to Azure Container Apps using the included infrastructure as code.

### Prerequisites
- Azure subscription
- Azure CLI installed
- CIAM tenant (see [infra/ENTRA-EXTERNAL-ID-SETUP.md](infra/ENTRA-EXTERNAL-ID-SETUP.md))

### Quick Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for comprehensive deployment instructions.

```bash
# 1. Get tenant ID
./infra/get-tenant-id.sh rg-fencemark-identity-dev

# 2. Update dev.bicepparam with tenant ID

# 3. Deploy infrastructure
az deployment sub create \
  --location australiaeast \
  --template-file ./infra/main.bicep \
  --parameters ./infra/dev.bicepparam \
  --name main

# 4. Grant Key Vault access
./infra/grant-keyvault-access.sh rg-fencemark-dev main
```

See [infra/README.md](infra/README.md) for infrastructure details.

## Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor)
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
