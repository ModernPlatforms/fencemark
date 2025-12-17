# Azure App Configuration and Key Vault Setup

This document describes how Fencemark uses Azure App Configuration and Azure Key Vault for centralized configuration and secret management.

## Overview

Fencemark uses a centralized configuration approach:

- **Central Azure App Configuration**: A single App Config store in its own resource group (`rg-fencemark-central-config`) that serves all environments (dev, staging, prod) using labels
- **Per-Environment Azure Key Vault**: Each environment has its own Key Vault (in `rg-fencemark-dev`, `rg-fencemark-staging`, `rg-fencemark-prod`) for environment-specific secrets
- **Managed Identity**: Both API and Web services use system-assigned managed identities to access App Config and Key Vault without credentials

## Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                        Central Configuration                         │
│                  (rg-fencemark-central-config)                       │
│                                                                      │
│                    ┌────────────────────────┐                       │
│                    │  Central App Config    │                       │
│                    │  - dev label           │                       │
│                    │  - staging label       │                       │
│                    │  - prod label          │                       │
│                    └───────────┬────────────┘                       │
│                                │                                     │
└────────────────────────────────┼─────────────────────────────────────┘
                                 │
        ┌────────────────────────┼────────────────────────┐
        │                        │                        │
        ▼                        ▼                        ▼
┌───────────────┐       ┌───────────────┐       ┌───────────────┐
│  Dev Env      │       │ Staging Env   │       │  Prod Env     │
│               │       │               │       │               │
│  ┌─────────┐  │       │  ┌─────────┐  │       │  ┌─────────┐  │
│  │API/Web  │  │       │  │API/Web  │  │       │  │API/Web  │  │
│  │Services │  │       │  │Services │  │       │  │Services │  │
│  └────┬────┘  │       │  └────┬────┘  │       │  └────┬────┘  │
│       │       │       │       │       │       │       │       │
│       │       │       │       │       │       │       │       │
│       ▼       │       │       ▼       │       │       ▼       │
│  ┌─────────┐  │       │  ┌─────────┐  │       │  ┌─────────┐  │
│  │Key Vault│  │       │  │Key Vault│  │       │  │Key Vault│  │
│  │  (Dev)  │  │       │  │(Staging)│  │       │  │ (Prod)  │  │
│  └─────────┘  │       │  └─────────┘  │       │  └─────────┘  │
│               │       │               │       │               │
└───────────────┘       └───────────────┘       └───────────────┘

┌─────────────────────────────────────────────────────────────┐
│                  Local Development                          │
│                                                             │
│  ┌──────────────┐     ┌──────────────┐                    │
│  │  API Service │     │ Web Frontend │                    │
│  │              │     │              │                    │
│  └──────┬───────┘     └──────┬───────┘                    │
│         │                    │                             │
│         │                    │                             │
│         └─────────┬──────────┘                             │
│                   │                                        │
│                   ▼                                        │
│         ┌──────────────────┐                              │
│         │  Aspire AppHost  │                              │
│         │  (appsettings)   │                              │
│         └──────────────────┘                              │
└─────────────────────────────────────────────────────────────┘
```

## What Gets Stored Where

### App Configuration (Environment-specific with labels)

All configuration values are stored with environment labels (dev, staging, prod):

- `ConnectionStrings:DefaultConnection` - SQL connection string
- `AzureMaps:SubscriptionKey` - Key Vault reference to Maps API key
- `AzureMaps:ClientId` - Maps account resource ID
- `AzureAd:Instance` - Entra External ID instance URL
- `AzureAd:TenantId` - Entra External ID tenant ID
- `AzureAd:ClientId` - Entra External ID client ID
- `AzureAd:Domain` - Entra External ID domain
- `KeyVault:Url` - External ID Key Vault URL (for certificates)
- `KeyVault:CertificateName` - Certificate name for auth

### Key Vault (Secrets)

Sensitive values stored in Key Vault:

- `sql-admin-password` - SQL Server admin password
- `azure-maps-primary-key` - Azure Maps subscription key

## Deployment Process

### First Time: Deploy Central App Configuration

Before deploying any environment, deploy the central App Config once:

```bash
cd infra
az deployment sub create \
  --location australiaeast \
  --template-file central-appconfig.bicep \
  --parameters central-appconfig.bicepparam
```

This creates:
- Resource group: `rg-fencemark-central-config`
- App Configuration: `appcs-fencemark`

### Environment Deployment

When you deploy each environment using `azd up` or bicep:

1. **Infrastructure Creation**:
   - Key Vault is created in the environment's resource group (e.g., `rg-fencemark-dev`)
   - Managed identities are created for API and Web services
   - Azure Maps account is created

2. **RBAC Assignments (Cross-Resource Group)**:
   - API Service managed identity gets "App Configuration Data Reader" role on central App Config
   - Web Frontend managed identity gets "App Configuration Data Reader" role on central App Config
   - Both identities get "Key Vault Secrets User" role on the environment's Key Vault

3. **Configuration Population**:
   - Azure Maps primary key is retrieved and stored in the environment's Key Vault
   - All configuration values are stored in central App Config with the environment label
   - Key Vault references in App Config point to the environment-specific Key Vault

4. **Application Startup**:
   - Containers start with `AppConfig__Endpoint` (central) and `AppConfig__Label` (environment) variables
   - Applications use DefaultAzureCredential (managed identity) to connect
   - Configuration is loaded from central App Config filtered by label
   - Key Vault references are resolved using the environment's Key Vault

## Local Development with Aspire

When running locally via `dotnet run --project fencemark.AppHost`:

- `AppConfig__Endpoint` is NOT set (absent from environment)
- App Configuration integration is skipped
- Configuration comes from local `appsettings.json` files
- Aspire provides service discovery and orchestration

This allows developers to work locally without Azure dependencies.

## Adding New Configuration Values

### To add a new configuration key:

1. **Update `main.bicep`** - Add a new module call:
   ```bicep
   module appConfigMyNewKey './modules/app-config-key-value.bicep' = {
     name: 'appConfigMyNewKey'
     scope: rg
     params: {
       appConfigName: appConfig.outputs.name
       key: 'MySection:MyKey'
       value: myValue
       label: environmentName
       contentType: 'text/plain'
     }
     dependsOn: [appConfig]
   }
   ```

2. **For secrets**, first store in Key Vault, then reference from App Config:
   ```bicep
   // Store in Key Vault
   module mySecretInKeyVault './modules/keyvault-secret.bicep' = {
     name: 'mySecretInKeyVault'
     scope: rg
     params: {
       keyVaultName: keyVault.outputs.name
       secretName: 'my-secret'
       secretValue: mySecretValue
       contentType: 'My secret description'
       tags: defaultTags
     }
     dependsOn: [keyVault]
   }

   // Reference from App Config
   module appConfigMySecret './modules/app-config-key-value.bicep' = {
     name: 'appConfigMySecret'
     scope: rg
     params: {
       appConfigName: appConfig.outputs.name
       key: 'MySection:MySecret'
       value: '{"uri":"${keyVault.outputs.vaultUri}secrets/my-secret"}'
       label: environmentName
       contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
     }
     dependsOn: [appConfig, mySecretInKeyVault]
   }
   ```

3. **Update parameter files** if the value comes from a parameter

4. **Deploy** - Run `azd up` to deploy changes

## Accessing Configuration in Code

### .NET Applications

Configuration is automatically loaded via ServiceDefaults. Access via `IConfiguration`:

```csharp
var myValue = builder.Configuration["MySection:MyKey"];
var mySecret = builder.Configuration["MySection:MySecret"]; // Transparently resolved from Key Vault
```

### Environment-Specific Values

Different values per environment are handled by labels:
- Dev uses label "dev"
- Staging uses label "staging"
- Prod uses label "prod"

The `AppConfig__Label` environment variable (set by bicep) controls which label is read.

## Manual Configuration Updates

You can manually update App Config values in the Azure Portal:

1. Navigate to App Configuration resource
2. Go to "Configuration explorer"
3. Filter by your environment label
4. Edit values directly

Changes are reflected in applications after they restart or refresh configuration.

## Security Benefits

1. **No secrets in code or environment variables** - Sensitive values only in Key Vault
2. **Managed identity authentication** - No connection strings or keys to manage
3. **RBAC-based access** - Fine-grained control over who can read/write config
4. **Audit trail** - All access to secrets is logged in Azure
5. **Separation of concerns** - Config managed separately from code deployment

## Troubleshooting

### "Unable to connect to App Configuration"

- Check managed identity has "App Configuration Data Reader" role
- Verify `AppConfig__Endpoint` environment variable is set correctly
- Check network connectivity from container to App Config

### "Key Vault reference failed to resolve"

- Check managed identity has "Key Vault Secrets User" role
- Verify the secret exists in Key Vault
- Ensure Key Vault reference format is correct in App Config

### "Local development not working"

- Ensure `AppConfig__Endpoint` is NOT set locally (should be absent)
- Configuration should come from local `appsettings.json`
- Check Aspire is running correctly

## References

- [Azure App Configuration documentation](https://docs.microsoft.com/azure/azure-app-configuration/)
- [Azure Key Vault documentation](https://docs.microsoft.com/azure/key-vault/)
- [Managed Identity documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [.NET Aspire configuration](https://learn.microsoft.com/dotnet/aspire/fundamentals/configuration)
