#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# deploy.sh — Deploy ContosoUniversity Azure infrastructure (Bicep)
#
# Usage:
#   ./deploy.sh [options]
#
# Options:
#   -g <resource-group>   Resource group name        (default: app-mod-cli-full-uni)
#   -l <location>         Azure region               (default: swedencentral)
#   -a <app-name>         App name prefix            (default: contoso-uni)
#   -e <env-name>         Environment name           (default: prod)
#   -u <sql-admin-login>  SQL admin login             (default: sqladmin)
#   -p <sql-password>     SQL admin password          (reads SQLPASSWORD env var if not given)
#   -o <entra-object-id>  Azure AD admin object ID    (optional, auto-detected from az login)
#   -n <entra-login>      Azure AD admin login name   (optional)
#
# Environment variables:
#   SQLPASSWORD   SQL admin password (alternative to -p flag)
# ---------------------------------------------------------------------------
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ─── Defaults ─────────────────────────────────────────────────────────────────
RESOURCE_GROUP="app-mod-cli-full-uni"
LOCATION="swedencentral"
APP_NAME="contoso-uni"
ENV_NAME="prod"
SQL_ADMIN_LOGIN="sqladmin"
SQL_ADMIN_PASSWORD="${SQLPASSWORD:-}"
SQL_ENTRA_OID=""
SQL_ENTRA_LOGIN=""

# ─── Parse flags ──────────────────────────────────────────────────────────────
while getopts "g:l:a:e:u:p:o:n:" opt; do
  case $opt in
    g) RESOURCE_GROUP="$OPTARG" ;;
    l) LOCATION="$OPTARG" ;;
    a) APP_NAME="$OPTARG" ;;
    e) ENV_NAME="$OPTARG" ;;
    u) SQL_ADMIN_LOGIN="$OPTARG" ;;
    p) SQL_ADMIN_PASSWORD="$OPTARG" ;;
    o) SQL_ENTRA_OID="$OPTARG" ;;
    n) SQL_ENTRA_LOGIN="$OPTARG" ;;
    *) echo "Unknown option -$OPTARG"; exit 1 ;;
  esac
done

# ─── Helpers ──────────────────────────────────────────────────────────────────
step()  { echo; echo "► $*"; }
ok()    { echo "  ✔ $*"; }
warn()  { echo "  ⚠ $*"; }

# ─── 1. Pre-flight checks ─────────────────────────────────────────────────────
step "Pre-flight checks"
command -v az >/dev/null 2>&1 || { echo "Azure CLI (az) is not installed."; exit 1; }

ACCOUNT_JSON=$(az account show 2>&1) || { echo "Not logged in. Run 'az login' first."; exit 1; }
ACCOUNT_NAME=$(echo "$ACCOUNT_JSON" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['user']['name'])" 2>/dev/null || echo "unknown")
SUBSCRIPTION_ID=$(echo "$ACCOUNT_JSON" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['id'])" 2>/dev/null || echo "unknown")
ok "Logged in as: $ACCOUNT_NAME  ($SUBSCRIPTION_ID)"

# Prompt for SQL password if not provided
if [ -z "$SQL_ADMIN_PASSWORD" ]; then
  read -r -s -p "Enter SQL administrator password: " SQL_ADMIN_PASSWORD
  echo
fi

# Auto-detect Entra admin
if [ -z "$SQL_ENTRA_OID" ]; then
  OID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || true)
  if [ -n "$OID" ]; then
    SQL_ENTRA_OID="$OID"
    SQL_ENTRA_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv 2>/dev/null || echo "")
    ok "Entra SQL admin: $SQL_ENTRA_LOGIN"
  else
    warn "Could not detect current Entra user. SQL Entra admin will not be configured."
  fi
fi

# ─── 2. Create resource group ─────────────────────────────────────────────────
step "Ensuring resource group '$RESOURCE_GROUP' in '$LOCATION'"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none
ok "Resource group ready."

# ─── 3. Deploy Bicep ──────────────────────────────────────────────────────────
DEPLOY_NAME="contoso-uni-infra-$(date +%Y%m%d%H%M)"
step "Deploying Bicep template as '$DEPLOY_NAME' (may take 5-10 minutes)..."

EXTRA_PARAMS=()
if [ -n "$SQL_ENTRA_OID" ]; then
  EXTRA_PARAMS+=(
    "sqlEntraAdminObjectId=$SQL_ENTRA_OID"
    "sqlEntraAdminLogin=$SQL_ENTRA_LOGIN"
  )
fi

DEPLOY_OUTPUT=$(az deployment group create \
  --name "$DEPLOY_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$SCRIPT_DIR/main.bicep" \
  --parameters \
      "appName=$APP_NAME" \
      "environmentName=$ENV_NAME" \
      "location=$LOCATION" \
      "sqlAdminLogin=$SQL_ADMIN_LOGIN" \
      "sqlAdminPassword=$SQL_ADMIN_PASSWORD" \
      "${EXTRA_PARAMS[@]:-}" \
  --output json)

ok "Deployment completed."

# ─── 4. Extract outputs ───────────────────────────────────────────────────────
step "Extracting outputs"
get_output() { echo "$DEPLOY_OUTPUT" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['properties']['outputs']['$1']['value'])"; }

APP_SERVICE_NAME=$(get_output appServiceName)
APP_SERVICE_HOST=$(get_output appServiceHostName)
APP_SERVICE_PID=$(get_output appServicePrincipalId)
SQL_SERVER_NAME=$(get_output sqlServerName)
SQL_SERVER_FQDN=$(get_output sqlServerFqdn)
SQL_DB_NAME=$(get_output sqlDatabaseName)
SB_NS_NAME=$(get_output serviceBusNamespaceName)
SB_NS_FQDN=$(get_output serviceBusNamespaceFqdn)
SB_QUEUE=$(get_output serviceBusQueueName)
STORAGE_NAME=$(get_output storageAccountName)
STORAGE_URI=$(get_output storageAccountUri)
BLOB_CONTAINER=$(get_output blobContainerName)
APPCONFIG_NAME=$(get_output appConfigName)
APPCONFIG_ENDPOINT=$(get_output appConfigEndpoint)

ok "App Service       : $APP_SERVICE_NAME  →  https://$APP_SERVICE_HOST"
ok "SQL Server        : $SQL_SERVER_FQDN"
ok "Service Bus       : $SB_NS_FQDN"
ok "Storage Account   : $STORAGE_URI"
ok "App Configuration : $APPCONFIG_ENDPOINT"

# ─── 5. Populate Azure App Configuration ─────────────────────────────────────
step "Populating Azure App Configuration"
CONFIG_FILE="$SCRIPT_DIR/../.azure/configuration-migration.json"
if [ -f "$CONFIG_FILE" ]; then
  python3 - <<PYEOF
import json, subprocess, sys

with open("$CONFIG_FILE") as f:
    data = json.load(f)

replacements = {
    "\${SERVICE_BUS_NAMESPACE}.servicebus.windows.net": "$SB_NS_FQDN",
    "https://<YOUR_STORAGE_ACCOUNT_NAME>.blob.core.windows.net": "$STORAGE_URI".rstrip("/"),
    "<YOUR_SERVER>.database.windows.net": "$SQL_SERVER_FQDN",
}

for kv in data.get("keyValues", []):
    value = kv["value"]
    for placeholder, real in replacements.items():
        value = value.replace(placeholder, real)
    if kv["key"] == "ConnectionStrings:DefaultConnection":
        value = f"Server=tcp:$SQL_SERVER_FQDN;Database=$SQL_DB_NAME;Authentication=Active Directory Default;TrustServerCertificate=True"

    result = subprocess.run(
        ["az", "appconfig", "kv", "set",
         "--name", "$APPCONFIG_NAME",
         "--key", kv["key"],
         "--value", value,
         "--yes", "--output", "none"],
        capture_output=True
    )
    status = "✔" if result.returncode == 0 else "✗"
    print(f"  {status}  {kv['key']}")
PYEOF
  ok "App Configuration populated."
else
  warn "configuration-migration.json not found. Skipping App Config population."
fi

# ─── 6. SQL MI instructions ───────────────────────────────────────────────────
step "SQL Managed Identity setup (MANUAL STEP)"
echo "  Run the following T-SQL against '$SQL_DB_NAME' on '$SQL_SERVER_FQDN':"
cat <<EOF
  CREATE USER [$APP_SERVICE_NAME] FROM EXTERNAL PROVIDER;
  ALTER ROLE db_datareader ADD MEMBER [$APP_SERVICE_NAME];
  ALTER ROLE db_datawriter ADD MEMBER [$APP_SERVICE_NAME];
  ALTER ROLE db_ddladmin   ADD MEMBER [$APP_SERVICE_NAME];
EOF

# ─── 7. Generate infra-config.md ─────────────────────────────────────────────
step "Generating infra-config.md"
cat > "$SCRIPT_DIR/infra-config.md" <<MDEOF
# Azure Resources Config

## Environment Info

| Property | Value |
|----------|-------|
| Subscription ID | \`$SUBSCRIPTION_ID\` |
| Resource Group | \`$RESOURCE_GROUP\` |
| Location | \`$LOCATION\` |

## Resource List

| Resource Type | Name | Region | Config Details |
|---------------|------|--------|----------------|
| App Service Plan | \`asp-$APP_NAME-$ENV_NAME\` | $LOCATION | Linux, B2 Basic tier |
| App Service | \`$APP_SERVICE_NAME\` | $LOCATION | FQDN: $APP_SERVICE_HOST, Managed Identity: $APP_SERVICE_PID |
| Azure SQL Server | \`$SQL_SERVER_NAME\` | $LOCATION | FQDN: $SQL_SERVER_FQDN |
| Azure SQL Database | \`$SQL_DB_NAME\` | $LOCATION | Server: $SQL_SERVER_FQDN, DB: $SQL_DB_NAME |
| Service Bus Namespace | \`$SB_NS_NAME\` | $LOCATION | FQDN: $SB_NS_FQDN |
| Service Bus Queue | \`$SB_QUEUE\` | $LOCATION | Namespace: $SB_NS_NAME |
| Storage Account | \`$STORAGE_NAME\` | $LOCATION | Blob URI: $STORAGE_URI |
| Blob Container | \`$BLOB_CONTAINER\` | $LOCATION | Storage: $STORAGE_NAME |
| App Configuration | \`$APPCONFIG_NAME\` | $LOCATION | Endpoint: $APPCONFIG_ENDPOINT |
MDEOF
ok "infra-config.md written."

echo
echo "✅ Infrastructure provisioning complete!"
echo "   App URL: https://$APP_SERVICE_HOST"
