#!/bin/bash
# ============================================================================
# Update Container App with Entra External ID Settings
# ============================================================================
# This script updates the Web Frontend Container App with the application
# (client) ID after the Entra External ID deployment completes.
#
# Usage:
#   ./update-entra-settings.sh <resource-group> <deployment-name>
#
# Example:
#   ./update-entra-settings.sh rg-fencemark-dev main
# ============================================================================

set -e

# Check arguments
if [ $# -lt 1 ]; then
    echo "Usage: $0 <resource-group> [deployment-name]"
    echo "Example: $0 rg-fencemark-dev main"
    exit 1
fi

RESOURCE_GROUP=$1
DEPLOYMENT_NAME=${2:-main}

echo "============================================================================"
echo "Updating Container App with Entra External ID Settings"
echo "============================================================================"
echo "Resource Group: $RESOURCE_GROUP"
echo "Deployment Name: $DEPLOYMENT_NAME"
echo ""

# Get deployment outputs
echo "Retrieving deployment outputs..."
APPLICATION_ID=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query "properties.outputs.entraExternalIdApplicationId.value" \
    -o tsv)

WEB_FRONTEND_NAME=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query "properties.outputs.webFrontendName.value" \
    -o tsv)

# Check if Entra External ID is enabled
if [ -z "$APPLICATION_ID" ] || [ "$APPLICATION_ID" == "null" ] || [ "$APPLICATION_ID" == "" ]; then
    echo "Error: Entra External ID is not enabled or application ID not found"
    echo "Please ensure enableEntraExternalId is set to true in your parameters"
    exit 1
fi

echo "Application (Client) ID: $APPLICATION_ID"
echo "Web Frontend Name: $WEB_FRONTEND_NAME"
echo ""

# Update Container App environment variables
echo "Updating Container App environment variables..."
az containerapp update \
    --name "$WEB_FRONTEND_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --set-env-vars "AzureAd__ClientId=$APPLICATION_ID" \
    --output none

echo ""
echo "============================================================================"
echo "✓ Container App updated successfully"
echo "============================================================================"
echo ""
echo "Next steps:"
echo "1. Verify the application is working by visiting the Web Frontend URL"
echo "2. Test the sign-in flow"
echo "3. Check application logs for any authentication errors"
echo ""
echo "To get the Web Frontend URL:"
echo "  az deployment group show \\"
echo "    --resource-group $RESOURCE_GROUP \\"
echo "    --name $DEPLOYMENT_NAME \\"
echo "    --query properties.outputs.webFrontendUrl.value -o tsv"
echo ""
