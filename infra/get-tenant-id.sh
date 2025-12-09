#!/bin/bash
# ============================================================================
# Get Tenant ID from Azure Entra External ID Deployment
# ============================================================================
# This script retrieves the tenant ID from an existing Entra External ID
# (CIAM) deployment and updates the dev.bicepparam file.
#
# Usage:
#   ./get-tenant-id.sh <resource-group>
#
# Example:
#   ./get-tenant-id.sh rg-fencemark-identity-dev
# ============================================================================

set -e

# Check arguments
if [ $# -lt 1 ]; then
    echo "Usage: $0 <resource-group>"
    echo "Example: $0 rg-fencemark-identity-dev"
    exit 1
fi

RESOURCE_GROUP=$1
DEPLOYMENT_NAME=${2:-ciamDirectory}

echo "============================================================================"
echo "Retrieving Tenant ID from Entra External ID Deployment"
echo "============================================================================"
echo "Resource Group: $RESOURCE_GROUP"
echo "Deployment Name: $DEPLOYMENT_NAME"
echo ""

# Try to get the tenant ID from the deployment outputs
echo "Checking for deployment outputs..."
TENANT_ID=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query "properties.outputs.tenantId.value" \
    -o tsv 2>/dev/null || echo "")

# If not found in deployment, try to get it from the CIAM directory resource
if [ -z "$TENANT_ID" ] || [ "$TENANT_ID" == "null" ]; then
    echo "Tenant ID not found in deployment outputs, checking CIAM directory resources..."
    
    # List all CIAM directories in the resource group
    CIAM_RESOURCES=$(az resource list \
        --resource-group "$RESOURCE_GROUP" \
        --resource-type "Microsoft.AzureActiveDirectory/ciamDirectories" \
        --query "[].{name:name, tenantId:properties.tenantId}" \
        -o json)
    
    # Extract tenant ID from the first CIAM directory found
    TENANT_ID=$(echo "$CIAM_RESOURCES" | jq -r '.[0].tenantId' 2>/dev/null || echo "")
fi

if [ -z "$TENANT_ID" ] || [ "$TENANT_ID" == "null" ] || [ "$TENANT_ID" == "" ]; then
    echo "❌ Error: Could not find tenant ID"
    echo ""
    echo "Please verify:"
    echo "1. The CIAM tenant has been deployed to resource group: $RESOURCE_GROUP"
    echo "2. You have access to the Azure subscription"
    echo "3. The az CLI is authenticated (run: az login)"
    echo ""
    echo "Alternatively, you can manually find the tenant ID in the Azure Portal:"
    echo "1. Go to Azure Portal > Microsoft Entra ID"
    echo "2. Switch to your CIAM tenant (devfencemark.onmicrosoft.com)"
    echo "3. Overview page will show the Tenant ID"
    echo ""
    exit 1
fi

echo "✓ Tenant ID found: $TENANT_ID"
echo ""

# Display the value
echo "============================================================================"
echo "Tenant ID: $TENANT_ID"
echo "============================================================================"
echo ""
echo "Update your dev.bicepparam file with this value:"
echo "  param entraExternalIdTenantId = '$TENANT_ID'"
echo ""
