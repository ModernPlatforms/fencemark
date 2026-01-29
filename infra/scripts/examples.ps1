# ============================================================================
# Example commands for pushing App Configuration per environment
# ============================================================================

# Dev environment
.\Push-AppConfiguration.ps1 `
    -Environment dev `
    -KeyVaultName "kv-fencemark-dev" `
    -CorsOrigins "https://localhost:7173,https://dev.fencemark.com.au"

# Staging environment
.\Push-AppConfiguration.ps1 `
    -Environment staging `
    -KeyVaultName "kv-fencemark-staging" `
    -CorsOrigins "https://staging.fencemark.com.au"

# Production environment
.\Push-AppConfiguration.ps1 `
    -Environment prod `
    -KeyVaultName "kv-fencemark-prod" `
    -CorsOrigins "https://fencemark.com.au"

# ============================================================================
# To skip cadastral settings (if already configured):
# ============================================================================
.\Push-AppConfiguration.ps1 `
    -Environment dev `
    -KeyVaultName "kv-fencemark-dev" `
    -CorsOrigins "https://localhost:7173" `
    -SkipCadastral

# ============================================================================
# View current configuration for an environment:
# ============================================================================
# az appconfig kv list --name appcs-fencemark --label dev --output table
# az appconfig kv list --name appcs-fencemark --label staging --output table
# az appconfig kv list --name appcs-fencemark --label prod --output table
