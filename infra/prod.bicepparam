using './main.bicep'

// ============================================================================
// PROD Environment Parameters
// ============================================================================

param environmentName = 'prod'
param location = readEnvironmentVariable('AZURE_LOCATION', 'australiaeast')
param resourceGroupName = 'rg-fencemark-prod'

// ============================================================================
// Container Images
// ============================================================================

param apiServiceImage = ''
param webFrontendImage = ''

// ============================================================================
// Resource Scaling (Prod - production-ready resources)
// ============================================================================

param apiServiceCpu = '1'
param apiServiceMemory = '2Gi'
param apiServiceMinReplicas = 2
param apiServiceMaxReplicas = 10

param webFrontendCpu = '1'
param webFrontendMemory = '2Gi'
param webFrontendMinReplicas = 2
param webFrontendMaxReplicas = 10
param customDomain = 'fencemark.com.au'

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
  environment: 'prod'
}
