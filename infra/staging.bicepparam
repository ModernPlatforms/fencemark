using './main.bicep'

// ============================================================================
// STAGING Environment Parameters
// ============================================================================

param environmentName = 'staging'
param location = readEnvironmentVariable('AZURE_LOCATION', 'australiaeast')
param resourceGroupName = 'rg-fencemark-staging'

// ============================================================================
// Container Images
// ============================================================================

param apiServiceImage = ''
param webFrontendImage = ''

// ============================================================================
// Resource Scaling (Staging - moderate resources)
// ============================================================================

param apiServiceCpu = '0.5'
param apiServiceMemory = '1Gi'
param apiServiceMinReplicas = 1
param apiServiceMaxReplicas = 3

param webFrontendCpu = '0.5'
param webFrontendMemory = '1Gi'
param webFrontendMinReplicas = 1
param webFrontendMaxReplicas = 3
param customDomain = 'stgfencemark.modernplatforms.dev'

// ============================================================================
// Static Web App Configuration
// ============================================================================

param deployStaticWebApp = true
param staticWebAppSku = 'Standard'
param staticWebAppCustomDomain = 'app.stgfencemark.modernplatforms.dev'

// ============================================================================
// Custom Domain Configuration
// ============================================================================
// Set to true after initial deployment once certificate is validated
param bindCustomDomainCertificate = false

// ============================================================================
// Central App Configuration
// ============================================================================
// The central App Config is deployed separately and shared across all environments
param centralAppConfigName = 'appcs-fencemark'
param centralAppConfigResourceGroup = 'rg-fencemark-central-config'

// ============================================================================
// Azure Entra External ID Authentication
// ============================================================================
// NOTE: These values are stored in Azure App Configuration during deployment.
// Applications use managed identity to read from App Config at runtime.
// Configure these parameters with your Entra External ID (CIAM) tenant details
param entraExternalIdTenantId = ''       // Run ./infra/get-tenant-id.sh to retrieve
param entraExternalIdClientId = ''       // Application (client) ID
param entraExternalIdInstance = ''       // e.g., 'https://yourtenant.ciamlogin.com/'
param entraExternalIdDomain = ''         // e.g., 'yourtenant.onmicrosoft.com'
param keyVaultUrl = ''                   // External ID Key Vault URL for certificates
param certificateName = ''               // Certificate name in Key Vault
param externalidRg = ''                  // External ID resource group name

// ============================================================================
// SQL Database - Managed Identity Authentication
// ============================================================================
// To enable managed identity for SQL:
// 1. Set useManagedIdentityForSql = true
// 2. Provide sqlAzureAdAdminObjectId (object ID of an Azure AD user/group who can manage DB users)
// 3. Provide sqlAzureAdAdminLogin (the UPN or group name)
// 4. After deployment, run the SQL script in infra/scripts/create-sql-user.sql to create the app identity

param useManagedIdentityForSql = false  // Set to true after configuring Azure AD admin
param sqlAzureAdAdminObjectId = ''       // Get from: az ad user show --id <email> --query id -o tsv
param sqlAzureAdAdminLogin = ''          // e.g., 'admin@yourdomain.com'

// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: 'staging'
}
