#!/bin/bash

# ============================================================================
# Fencemark Rollback Script
# ============================================================================
# This script performs a quick rollback of Container Apps to previous revisions
# 
# Usage:
#   ./rollback.sh <environment> [revision-name]
#
# Examples:
#   ./rollback.sh dev                    # Rollback dev to previous revision
#   ./rollback.sh prod ca-apiservice--abc123  # Rollback prod API to specific revision
#
# Environments: dev, staging, prod
# ============================================================================

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if environment parameter is provided
if [ -z "$1" ]; then
    echo -e "${RED}Error: Environment parameter is required${NC}"
    echo "Usage: $0 <environment> [revision-name]"
    echo "Environments: dev, staging, prod"
    exit 1
fi

ENVIRONMENT=$1
SPECIFIC_REVISION=$2

# Set resource group based on environment
RESOURCE_GROUP="rg-fencemark-${ENVIRONMENT}"

echo -e "${BLUE}============================================================================${NC}"
echo -e "${BLUE}Fencemark Rollback Script - ${ENVIRONMENT} Environment${NC}"
echo -e "${BLUE}============================================================================${NC}"
echo ""

# Verify Azure CLI is logged in
if ! az account show &> /dev/null; then
    echo -e "${RED}Error: Not logged in to Azure CLI${NC}"
    echo "Please run: az login"
    exit 1
fi

# Get current subscription
SUBSCRIPTION=$(az account show --query name --output tsv)
echo -e "Azure Subscription: ${GREEN}${SUBSCRIPTION}${NC}"
echo -e "Resource Group: ${GREEN}${RESOURCE_GROUP}${NC}"
echo ""

# Verify resource group exists
if ! az group show --name "$RESOURCE_GROUP" &> /dev/null; then
    echo -e "${RED}Error: Resource group '${RESOURCE_GROUP}' not found${NC}"
    exit 1
fi

# Get container app names
echo -e "${BLUE}Fetching container app names...${NC}"
API_APP_NAME=$(az containerapp list --resource-group "$RESOURCE_GROUP" --query "[?contains(name, 'apiservice')].name" --output tsv | head -n 1)
WEB_APP_NAME=$(az containerapp list --resource-group "$RESOURCE_GROUP" --query "[?contains(name, 'webfrontend')].name" --output tsv | head -n 1)

if [ -z "$API_APP_NAME" ] || [ -z "$WEB_APP_NAME" ]; then
    echo -e "${RED}Error: Could not find container apps in resource group${NC}"
    exit 1
fi

echo -e "API Service: ${GREEN}${API_APP_NAME}${NC}"
echo -e "Web Frontend: ${GREEN}${WEB_APP_NAME}${NC}"
echo ""

# Function to get previous healthy revision
get_previous_revision() {
    local APP_NAME=$1
    
    # Get all revisions, sorted by creation time (newest first)
    local REVISIONS=$(az containerapp revision list \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "[?properties.active && properties.healthState=='Healthy'].{name:name, created:properties.createdTime, traffic:properties.trafficWeight}" \
        --output json)
    
    # Get the second revision (first is current, second is previous)
    local PREVIOUS=$(echo "$REVISIONS" | jq -r '.[1].name // empty')
    
    if [ -z "$PREVIOUS" ]; then
        echo -e "${RED}Error: No previous healthy revision found for ${APP_NAME}${NC}"
        return 1
    fi
    
    echo "$PREVIOUS"
}

# Function to rollback a container app
rollback_app() {
    local APP_NAME=$1
    local TARGET_REVISION=$2
    
    echo -e "${YELLOW}Rolling back ${APP_NAME}...${NC}"
    
    # If no specific revision provided, get previous healthy one
    if [ -z "$TARGET_REVISION" ]; then
        echo "Finding previous healthy revision..."
        TARGET_REVISION=$(get_previous_revision "$APP_NAME")
        if [ $? -ne 0 ]; then
            return 1
        fi
    fi
    
    echo -e "Target revision: ${GREEN}${TARGET_REVISION}${NC}"
    
    # Verify target revision exists and is healthy
    local REVISION_HEALTH=$(az containerapp revision show \
        --name "$TARGET_REVISION" \
        --resource-group "$RESOURCE_GROUP" \
        --query "properties.healthState" \
        --output tsv 2>/dev/null)
    
    if [ -z "$REVISION_HEALTH" ]; then
        echo -e "${RED}Error: Revision ${TARGET_REVISION} not found${NC}"
        return 1
    fi
    
    if [ "$REVISION_HEALTH" != "Healthy" ]; then
        echo -e "${YELLOW}Warning: Target revision is not healthy (Status: ${REVISION_HEALTH})${NC}"
        read -p "Continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            echo "Rollback cancelled"
            return 1
        fi
    fi
    
    # Activate the revision
    echo "Activating revision..."
    az containerapp revision activate \
        --name "$TARGET_REVISION" \
        --resource-group "$RESOURCE_GROUP" \
        --output none
    
    # Set 100% traffic to the revision
    echo "Setting traffic to 100%..."
    az containerapp ingress traffic set \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --revision-weight "${TARGET_REVISION}=100" \
        --output none
    
    echo -e "${GREEN}✓ Rollback complete for ${APP_NAME}${NC}"
    return 0
}

# Confirm rollback
echo -e "${YELLOW}⚠️  WARNING: This will rollback both API and Web services to previous revisions${NC}"
if [ -n "$SPECIFIC_REVISION" ]; then
    echo -e "Specific revision: ${SPECIFIC_REVISION}"
fi
echo ""
read -p "Are you sure you want to proceed? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Rollback cancelled${NC}"
    exit 0
fi

echo ""

# Perform rollback
echo -e "${BLUE}============================================================================${NC}"
echo -e "${BLUE}Starting Rollback${NC}"
echo -e "${BLUE}============================================================================${NC}"
echo ""

# Rollback API Service
rollback_app "$API_APP_NAME" "$SPECIFIC_REVISION"
API_RESULT=$?

echo ""

# Rollback Web Frontend
rollback_app "$WEB_APP_NAME" "$SPECIFIC_REVISION"
WEB_RESULT=$?

echo ""
echo -e "${BLUE}============================================================================${NC}"
echo -e "${BLUE}Rollback Summary${NC}"
echo -e "${BLUE}============================================================================${NC}"
echo ""

if [ $API_RESULT -eq 0 ]; then
    echo -e "API Service: ${GREEN}✓ Success${NC}"
else
    echo -e "API Service: ${RED}✗ Failed${NC}"
fi

if [ $WEB_RESULT -eq 0 ]; then
    echo -e "Web Frontend: ${GREEN}✓ Success${NC}"
else
    echo -e "Web Frontend: ${RED}✗ Failed${NC}"
fi

echo ""

# Health checks
if [ $API_RESULT -eq 0 ] && [ $WEB_RESULT -eq 0 ]; then
    echo -e "${BLUE}Running health checks...${NC}"
    sleep 10
    
    # Get App URLs
    API_FQDN=$(az containerapp show \
        --name "$API_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query properties.configuration.ingress.fqdn \
        --output tsv 2>/dev/null)
    
    WEB_FQDN=$(az containerapp show \
        --name "$WEB_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query properties.configuration.ingress.fqdn \
        --output tsv 2>/dev/null)
    
    # Check API health
    if [ -n "$API_FQDN" ]; then
        API_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" "https://${API_FQDN}/health" || echo "000")
        if [ "$API_HEALTH" = "200" ]; then
            echo -e "API Health Check: ${GREEN}✓ Healthy (HTTP ${API_HEALTH})${NC}"
        else
            echo -e "API Health Check: ${YELLOW}⚠ Unhealthy (HTTP ${API_HEALTH})${NC}"
        fi
    fi
    
    # Check Web health
    if [ -n "$WEB_FQDN" ]; then
        WEB_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" "https://${WEB_FQDN}/health" || echo "000")
        if [ "$WEB_HEALTH" = "200" ]; then
            echo -e "Web Health Check: ${GREEN}✓ Healthy (HTTP ${WEB_HEALTH})${NC}"
        else
            echo -e "Web Health Check: ${YELLOW}⚠ Unhealthy (HTTP ${WEB_HEALTH})${NC}"
        fi
    fi
    
    echo ""
    echo -e "${GREEN}Rollback completed successfully!${NC}"
    echo ""
    echo -e "${BLUE}Next Steps:${NC}"
    echo "1. Monitor application logs for any errors"
    echo "2. Verify critical user workflows"
    echo "3. Document the incident and root cause"
    echo "4. Plan fix for the issue that caused rollback"
    
    exit 0
else
    echo -e "${RED}Rollback partially failed. Please check the errors above and take manual action.${NC}"
    exit 1
fi
