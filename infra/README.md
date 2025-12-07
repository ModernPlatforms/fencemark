# Fencemark Infrastructure

This directory contains the Azure infrastructure as code (IaC) for deploying the Fencemark .NET Aspire application to Azure Container Apps.

## Architecture

The infrastructure deploys the following Azure resources:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Resource Group                                  │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                     Container Apps Environment                        │  │
│  │                                                                       │  │
│  │  ┌─────────────────────────┐      ┌─────────────────────────┐        │  │
│  │  │    Web Frontend         │      │    API Service          │        │  │
│  │  │    (External Ingress)   │─────▶│    (Internal Ingress)   │        │  │
│  │  │    Port: 8080           │      │    Port: 8080           │        │  │
│  │  │                         │      │                         │        │  │
│  │  │ /alive  - Liveness      │      │ /alive  - Liveness      │        │  │
│  │  │ /health - Readiness     │      │ /health - Readiness     │        │  │
│  │  └─────────────────────────┘      └─────────────────────────┘        │  │
│  │                                                                       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─────────────────────┐      ┌─────────────────────────────────────────┐  │
│  │  Container Registry │      │           Log Analytics Workspace       │  │
│  │  (Basic SKU)        │      │           (30-day retention)            │  │
│  └─────────────────────┘      └─────────────────────────────────────────┘  │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                       Azure Maps Account                            │    │
│  │                       (Gen2 / G2 SKU)                               │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Resources

| Resource | Purpose |
|----------|---------|
| Log Analytics Workspace | Centralized logging and monitoring |
| Container Registry | Store container images for the applications |
| Container Apps Environment | Managed environment for running Container Apps |
| API Service Container App | Backend API service (internal ingress) |
| Web Frontend Container App | Blazor Server frontend (external ingress) |
| Azure Maps Account | Location-based services (maps, geocoding, routing) |

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
- Azure subscription with appropriate permissions

## Deployment

### Option 1: Using Azure Developer CLI (Recommended)

```bash
# Login to Azure
azd auth login

# Initialize environment (first time only)
azd init

# Provision infrastructure and deploy
azd up
```

### Option 2: Using Azure CLI

```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-fencemark-dev --location eastus

# Deploy infrastructure
az deployment group create \
  --resource-group rg-fencemark-dev \
  --template-file main.bicep \
  --parameters main.bicepparam
```

### Option 3: Using Bicep CLI

```bash
# Deploy with Bicep
az deployment group create \
  --resource-group rg-fencemark-dev \
  --template-file main.bicep \
  --parameters environmentName=dev location=eastus
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `environmentName` | string | (required) | Name of the environment (dev, staging, prod) |
| `location` | string | resourceGroup().location | Azure region for resources |
| `tags` | object | {} | Additional tags for resources |
| `apiServiceImage` | string | '' | Container image for API service |
| `webFrontendImage` | string | '' | Container image for Web frontend |
| `apiServiceCpu` | string | '0.25' | CPU allocation for API service |
| `apiServiceMemory` | string | '0.5Gi' | Memory allocation for API service |
| `webFrontendCpu` | string | '0.25' | CPU allocation for Web frontend |
| `webFrontendMemory` | string | '0.5Gi' | Memory allocation for Web frontend |
| `apiServiceMinReplicas` | int | 0 | Minimum replicas for API service |
| `apiServiceMaxReplicas` | int | 3 | Maximum replicas for API service |
| `webFrontendMinReplicas` | int | 0 | Minimum replicas for Web frontend |
| `webFrontendMaxReplicas` | int | 3 | Maximum replicas for Web frontend |

## Outputs

| Output | Description |
|--------|-------------|
| `containerAppsEnvironmentName` | Name of the Container Apps Environment |
| `containerRegistryName` | Name of the Container Registry |
| `containerRegistryLoginServer` | Login server URL for the Container Registry |
| `logAnalyticsWorkspaceName` | Name of the Log Analytics Workspace |
| `apiServiceName` | Name of the API Service Container App |
| `apiServiceFqdn` | FQDN of the API Service Container App |
| `webFrontendName` | Name of the Web Frontend Container App |
| `webFrontendFqdn` | FQDN of the Web Frontend Container App |
| `webFrontendUrl` | Full URL of the Web Frontend |
| `mapsAccountName` | Name of the Azure Maps Account |
| `mapsAccountResourceId` | Resource ID of the Azure Maps Account |

## Azure Verified Modules

This infrastructure uses [Azure Verified Modules (AVM)](https://aka.ms/avm) from the Bicep public registry:

- `br/public:avm/res/operational-insights/workspace` - Log Analytics
- `br/public:avm/res/container-registry/registry` - Container Registry
- `br/public:avm/res/app/managed-environment` - Container Apps Environment
- `br/public:avm/res/app/container-app` - Container Apps

## Files

| File | Purpose |
|------|---------|
| `main.bicep` | Main infrastructure template |
| `main.bicepparam` | Parameters file for deployment |
| `abbreviations.json` | Azure resource naming abbreviations |
| `README.md` | This documentation file |

## Scaling

Both Container Apps are configured with autoscaling:

- **Minimum replicas**: 0 (scale to zero when idle)
- **Maximum replicas**: 3 (scale up under load)

Modify `apiServiceMinReplicas`, `apiServiceMaxReplicas`, `webFrontendMinReplicas`, and `webFrontendMaxReplicas` parameters for different scaling behavior.

## Health Probes

Both applications are configured with health probes:

- **Liveness probe**: `/alive` - Checks if the app is running
- **Readiness probe**: `/health` - Checks if the app is ready to receive traffic

These match the health check endpoints configured in the .NET Aspire AppHost.

## Cost Optimization

- Container Apps use consumption plan (pay-per-use)
- Scale to zero when no traffic
- Basic SKU for Container Registry
- 30-day log retention (adjustable)
