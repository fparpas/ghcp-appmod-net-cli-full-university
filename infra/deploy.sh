#!/usr/bin/env bash
# deploy.sh — Deploy ContosoUniversity infrastructure to Azure
# Usage: ./infra/deploy.sh [options]
# Options:
#   -g  Resource group name        (required)
#   -l  Location                   (default: eastus)
#   -s  Subscription ID            (required)
#   -p  SQL admin password         (required)
#   -a  AAD admin login            (required)
#   -o  AAD admin object ID        (required)
#   -t  AAD tenant ID              (required)
#   -e  Environment name           (default: dev)
#   -n  App name                   (default: contosouniv)

set -euo pipefail

RESOURCE_GROUP_NAME=""
LOCATION="eastus"
SUBSCRIPTION_ID=""
SQL_ADMIN_PASSWORD=""
AAD_ADMIN_LOGIN=""
AAD_ADMIN_OBJECT_ID=""
AAD_TENANT_ID=""
ENVIRONMENT_NAME="dev"
APP_NAME="contosouniv"

usage() {
  echo "Usage: $0 -g <ResourceGroupName> -s <SubscriptionId> -p <SqlAdminPassword> \\"
  echo "          -a <AadAdminLogin> -o <AadAdminObjectId> -t <AadTenantId> \\"
  echo "          [-l <Location>] [-e <EnvironmentName>] [-n <AppName>]"
  exit 1
}

while getopts "g:l:s:p:a:o:t:e:n:" opt; do
  case $opt in
    g) RESOURCE_GROUP_NAME="$OPTARG" ;;
    l) LOCATION="$OPTARG" ;;
    s) SUBSCRIPTION_ID="$OPTARG" ;;
    p) SQL_ADMIN_PASSWORD="$OPTARG" ;;
    a) AAD_ADMIN_LOGIN="$OPTARG" ;;
    o) AAD_ADMIN_OBJECT_ID="$OPTARG" ;;
    t) AAD_TENANT_ID="$OPTARG" ;;
    e) ENVIRONMENT_NAME="$OPTARG" ;;
    n) APP_NAME="$OPTARG" ;;
    *) usage ;;
  esac
done

[[ -z "$RESOURCE_GROUP_NAME" ]] && { echo "ERROR: -g ResourceGroupName is required"; usage; }
[[ -z "$SUBSCRIPTION_ID" ]]    && { echo "ERROR: -s SubscriptionId is required"; usage; }
[[ -z "$SQL_ADMIN_PASSWORD" ]] && { echo "ERROR: -p SqlAdminPassword is required"; usage; }
[[ -z "$AAD_ADMIN_LOGIN" ]]    && { echo "ERROR: -a AadAdminLogin is required"; usage; }
[[ -z "$AAD_ADMIN_OBJECT_ID" ]]&& { echo "ERROR: -o AadAdminObjectId is required"; usage; }
[[ -z "$AAD_TENANT_ID" ]]      && { echo "ERROR: -t AadTenantId is required"; usage; }

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== ContosoUniversity Infrastructure Deployment ==="
echo "Resource Group : $RESOURCE_GROUP_NAME"
echo "Location       : $LOCATION"
echo "Subscription   : $SUBSCRIPTION_ID"
echo "Environment    : $ENVIRONMENT_NAME"
echo ""

# Set subscription
echo ">> Setting subscription..."
az account set --subscription "$SUBSCRIPTION_ID"

# Create resource group
echo ">> Creating resource group '$RESOURCE_GROUP_NAME' in '$LOCATION'..."
az group create \
  --name "$RESOURCE_GROUP_NAME" \
  --location "$LOCATION" \
  --tags "application=contosouniversity" "environment=$ENVIRONMENT_NAME" "managedBy=bicep"

# Deploy Bicep template
echo ">> Deploying Bicep template..."
DEPLOY_OUTPUT=$(az deployment group create \
  --resource-group "$RESOURCE_GROUP_NAME" \
  --template-file "$SCRIPT_DIR/main.bicep" \
  --parameters "$SCRIPT_DIR/parameters.json" \
  --parameters \
    environmentName="$ENVIRONMENT_NAME" \
    location="$LOCATION" \
    appName="$APP_NAME" \
    sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
    aadAdminLogin="$AAD_ADMIN_LOGIN" \
    aadAdminObjectId="$AAD_ADMIN_OBJECT_ID" \
    aadTenantId="$AAD_TENANT_ID" \
  --output json)

echo ""
echo "=== Deployment Outputs ==="
echo "$DEPLOY_OUTPUT" | python3 -c "
import json, sys
data = json.load(sys.stdin)
outputs = data.get('properties', {}).get('outputs', {})
for k, v in outputs.items():
    print(f'  {k}: {v[\"value\"]}')
"

WEB_APP_NAME=$(echo "$DEPLOY_OUTPUT" | python3 -c "import json,sys; print(json.load(sys.stdin)['properties']['outputs']['webAppName']['value'])")
SQL_SERVER_FQDN=$(echo "$DEPLOY_OUTPUT" | python3 -c "import json,sys; print(json.load(sys.stdin)['properties']['outputs']['sqlServerFqdn']['value'])")

echo ""
echo "=== Post-Deployment: SQL Managed Identity Setup ==="
echo "Run the following SQL script against '$SQL_SERVER_FQDN' database to grant access:"
echo ""
echo "  CREATE USER [$WEB_APP_NAME] FROM EXTERNAL PROVIDER;"
echo "  ALTER ROLE db_owner ADD MEMBER [$WEB_APP_NAME];"
echo ""
echo "You can run it via Azure Portal Query Editor or sqlcmd with AAD authentication."
echo ""
echo "=== Deployment Complete ==="
