# Deployment Steps for App Config and Key Vault

This guide walks through deploying the Fencemark infrastructure with the new centralized App Configuration and per-environment Key Vault setup.

## Prerequisites

- Azure CLI installed and logged in
- Azure subscription with appropriate permissions
- .NET 10 SDK installed

## Step 1: Deploy Central App Configuration (One Time)

The central App Configuration is deployed once and shared across all environments.

```bash
cd infra

# Deploy central App Config
az deployment sub create \
  --location australiaeast \
  --template-file central-appconfig.bicep \
  --parameters central-appconfig.bicepparam \
  --name central-appconfig-deployment
```

This creates:
- Resource Group: `rg-fencemark-central-config`
- App Configuration: `appcs-fencemark`

**Note**: You only need to run this once, not for each environment.

## Step 2: Deploy Environment Infrastructure

Deploy each environment (dev, staging, prod) separately. Each environment gets:
- Its own Key Vault
- Its own Azure Maps account
- Its own SQL database
- Container Apps with managed identities
- Configuration populated in central App Config with environment label

### Deploy Dev Environment

```bash
# Set environment variables
export AZURE_ENV_NAME=dev
export AZURE_LOCATION=australiaeast

# Deploy using azd
azd up --environment dev

# Or using bicep directly
az deployment sub create \
  --location australiaeast \
  --template-file main.bicep \
  --parameters dev.bicepparam \
  --name fencemark-dev-deployment
```

### Deploy Staging Environment

```bash
export AZURE_ENV_NAME=staging
export AZURE_LOCATION=australiaeast

azd up --environment staging

# Or using bicep directly
az deployment sub create \
  --location australiaeast \
  --template-file main.bicep \
  --parameters staging.bicepparam \
  --name fencemark-staging-deployment
```

### Deploy Production Environment

```bash
export AZURE_ENV_NAME=prod
export AZURE_LOCATION=australiaeast

azd up --environment prod

# Or using bicep directly
az deployment sub create \
  --location australiaeast \
  --template-file main.bicep \
  --parameters prod.bicepparam \
  --name fencemark-prod-deployment
```

## Step 3: Verify Deployment

### Check Central App Configuration

```bash
# List configuration values for dev environment
az appconfig kv list \
  --name appcs-fencemark \
  --label dev \
  --query "[].{key:key, label:label}" \
  --output table

# List configuration values for staging environment
az appconfig kv list \
  --name appcs-fencemark \
  --label staging \
  --query "[].{key:key, label:label}" \
  --output table

# List configuration values for prod environment
az appconfig kv list \
  --name appcs-fencemark \
  --label prod \
  --query "[].{key:key, label:label}" \
  --output table
```

### Check Key Vault Secrets

```bash
# Dev environment
az keyvault secret list \
  --vault-name $(az keyvault list -g rg-fencemark-dev --query "[0].name" -o tsv) \
  --query "[].name" \
  --output table

# Staging environment
az keyvault secret list \
  --vault-name $(az keyvault list -g rg-fencemark-staging --query "[0].name" -o tsv) \
  --query "[].name" \
  --output table

# Production environment
az keyvault secret list \
  --vault-name $(az keyvault list -g rg-fencemark-prod --query "[0].name" -o tsv) \
  --query "[].name" \
  --output table
```

### Check Container App Health

```bash
# Dev
az containerapp show \
  --name $(az containerapp list -g rg-fencemark-dev --query "[?contains(name,'webfrontend')].name" -o tsv) \
  --resource-group rg-fencemark-dev \
  --query "properties.configuration.ingress.fqdn" \
  --output tsv

# Access the URL returned above to verify the application is running
```

## Step 4: Verify RBAC Permissions

Ensure managed identities have proper access:

```bash
# Check App Config role assignments
az role assignment list \
  --scope /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-fencemark-central-config/providers/Microsoft.AppConfiguration/configurationStores/appcs-fencemark \
  --query "[].{Principal:principalName, Role:roleDefinitionName}" \
  --output table

# Check Key Vault role assignments (dev example)
KV_NAME=$(az keyvault list -g rg-fencemark-dev --query "[0].name" -o tsv)
az role assignment list \
  --scope /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-fencemark-dev/providers/Microsoft.KeyVault/vaults/$KV_NAME \
  --query "[].{Principal:principalName, Role:roleDefinitionName}" \
  --output table
```

## Step 5: Test Application Configuration

The applications should automatically:
1. Connect to central App Config using managed identity
2. Load configuration with the environment-specific label
3. Resolve Key Vault references for secrets
4. Function normally without any code changes

### Test Configuration Loading

Check application logs to verify configuration is loaded:

```bash
# View API service logs
az containerapp logs show \
  --name $(az containerapp list -g rg-fencemark-dev --query "[?contains(name,'apiservice')].name" -o tsv) \
  --resource-group rg-fencemark-dev \
  --follow
```

Look for log entries indicating successful App Config connection and configuration loading.

## Troubleshooting

### Issue: "Unable to connect to App Configuration"

**Solution**: Check that:
1. The central App Config exists: `az appconfig show -n appcs-fencemark -g rg-fencemark-central-config`
2. Managed identity has "App Configuration Data Reader" role
3. `AppConfig__Endpoint` environment variable is set correctly on container apps

### Issue: "Key Vault reference failed to resolve"

**Solution**: Check that:
1. The Key Vault secret exists in the environment's Key Vault
2. Managed identity has "Key Vault Secrets User" role on the Key Vault
3. The Key Vault reference in App Config uses the correct format: `{"uri":"https://<vault-name>.vault.azure.net/secrets/<secret-name>"}`

### Issue: "Access denied to resource group"

**Solution**: 
1. Ensure you have appropriate Azure RBAC permissions
2. Check that the service principal or user has "Contributor" role on the subscription or resource groups

### Issue: "Configuration not updating in application"

**Solution**:
1. Restart the container app: `az containerapp revision restart`
2. Check if the correct label is being used: `AppConfig__Label` should match environment name
3. Verify configuration exists in App Config for that label

## Local Development

For local development via Aspire:

```bash
cd /path/to/fencemark
dotnet run --project fencemark.AppHost
```

Local development automatically:
- Skips Azure App Configuration (no `AppConfig__Endpoint` set)
- Uses local `appsettings.json` files
- Runs with Aspire orchestration

No Azure connectivity is required for local development.

## Updating Configuration

### Add a new configuration value

1. Add it to `main.bicep`:
```bicep
module appConfigMyNewSetting './modules/app-config-key-value.bicep' = {
  name: 'appConfigMyNewSetting-${environmentName}'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    key: 'MySection:MyKey'
    value: 'myValue'
    label: environmentName
    contentType: 'text/plain'
  }
  dependsOn: [centralAppConfig]
}
```

2. Redeploy the environment: `azd up`

3. Restart container apps to pick up new configuration

### Update an existing value manually

```bash
az appconfig kv set \
  --name appcs-fencemark \
  --key "MySection:MyKey" \
  --label dev \
  --value "newValue"
```

## Security Notes

- ✅ All secrets are stored in Key Vault, never in code or App Config directly
- ✅ Managed identity is used for authentication (no connection strings)
- ✅ RBAC controls access to configuration and secrets
- ✅ Each environment has isolated Key Vault for secrets
- ✅ Central App Config uses labels for environment separation

## Additional Resources

- [Azure App Configuration documentation](https://docs.microsoft.com/azure/azure-app-configuration/)
- [Azure Key Vault documentation](https://docs.microsoft.com/azure/key-vault/)
- [Managed Identity documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [.NET Aspire configuration](https://learn.microsoft.com/dotnet/aspire/fundamentals/configuration)
