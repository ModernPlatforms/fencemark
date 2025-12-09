#!/bin/bash
# ============================================================================
# Grant Key Vault Access to Web Frontend Managed Identity
# ============================================================================
# This script grants the Web Frontend's managed identity access to the
# Key Vault for certificate-based authentication.
#
# Usage:
#   ./grant-keyvault-access.sh <resource-group> <deployment-name>
#
# Example:
#   ./grant-keyvault-access.sh rg-fencemark-dev main
# ============================================================================

set -e

# Check arguments
if [ $# -lt 1 ]; then
    echo "Usage: $0 <resource-group> [deployment-name] [key-vault-name]"
    echo "Example: $0 rg-fencemark-dev main kv-ciambfwyw65gna5lu"
    exit 1
fi

RESOURCE_GROUP=$1
DEPLOYMENT_NAME=${2:-main}
KEY_VAULT_NAME=${3:-kv-ciambfwyw65gna5lu}

echo "============================================================================"
echo "Granting Key Vault Access to Web Frontend Managed Identity"
echo "============================================================================"
echo "Resource Group: $RESOURCE_GROUP"
echo "Deployment Name: $DEPLOYMENT_NAME"
echo "Key Vault: $KEY_VAULT_NAME"
echo ""

# Get the Web Frontend managed identity principal ID from deployment outputs
echo "Retrieving Web Frontend managed identity..."
IDENTITY_PRINCIPAL_ID=$(az deployment sub show \
    --name "$DEPLOYMENT_NAME" \
    --query "properties.outputs.webFrontendIdentityPrincipalId.value" \
    -o tsv 2>/dev/null || echo "")

if [ -z "$IDENTITY_PRINCIPAL_ID" ] || [ "$IDENTITY_PRINCIPAL_ID" == "null" ]; then
    echo "❌ Error: Could not find Web Frontend managed identity principal ID"
    echo ""
    echo "Please verify:"
    echo "1. The deployment has completed successfully"
    echo "2. The Web Frontend has a system-assigned managed identity enabled"
    echo "3. You have access to view deployment outputs"
    echo ""
    exit 1
fi

echo "✓ Managed Identity Principal ID: $IDENTITY_PRINCIPAL_ID"
echo ""

# Grant access to Key Vault
echo "Granting Key Vault access permissions..."
az keyvault set-policy \
    --name "$KEY_VAULT_NAME" \
    --object-id "$IDENTITY_PRINCIPAL_ID" \
    --certificate-permissions get list \
    --secret-permissions get list

echo ""
echo "============================================================================"
echo "✓ Key Vault Access Granted Successfully"
echo "============================================================================"
echo ""
echo "The Web Frontend managed identity now has access to:"
echo "- Certificate permissions: Get, List"
echo "- Secret permissions: Get, List (for certificate private keys)"
echo ""
echo "Next steps:"
echo "1. Verify the Web Frontend can access the certificate"
echo "2. Test the authentication flow"
echo "3. Check application logs for any authentication errors"
echo ""
