<#
.SYNOPSIS
    Deploys ContosoUniversity Azure infrastructure using Bicep.

.DESCRIPTION
    Provisions all Azure resources required by ContosoUniversity (.NET 10) in the
    SwedenCentral region using Azure CLI and Bicep templates. After provisioning,
    populates Azure App Configuration with values from .azure/configuration-migration.json.

.PARAMETER ResourceGroup
    Target Azure resource group name. Default: app-mod-cli-full-uni

.PARAMETER Location
    Azure region. Default: swedencentral

.PARAMETER AppName
    Application name prefix. Default: contoso-uni

.PARAMETER EnvironmentName
    Environment identifier. Default: prod

.PARAMETER SqlEntraAdminObjectId
    Object ID of the Azure AD user/group to set as SQL Entra admin.
    Auto-detected from the current signed-in user if not provided.

.PARAMETER SqlEntraAdminLogin
    Display name / UPN of the SQL Entra admin.
    Auto-detected from the current signed-in user if not provided.

.EXAMPLE
    .\deploy.ps1
    .\deploy.ps1 -ResourceGroup my-rg -Location swedencentral
    .\deploy.ps1 -SqlEntraAdminObjectId "<guid>" -SqlEntraAdminLogin "admin@contoso.com"
#>
[CmdletBinding()]
param(
    [string]$ResourceGroup    = 'app-mod-cli-full-uni',
    [string]$Location         = 'swedencentral',
    [string]$AppName          = 'contoso-uni',
    [string]$EnvironmentName  = 'prod',
    [string]$SqlEntraAdminObjectId = '',
    [string]$SqlEntraAdminLogin    = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir = $PSScriptRoot

# ─── Helpers ──────────────────────────────────────────────────────────────────
function Write-Step([string]$msg) { Write-Host "`n► $msg" -ForegroundColor Cyan }
function Write-OK([string]$msg)   { Write-Host "  ✔ $msg" -ForegroundColor Green }
function Write-Warn([string]$msg) { Write-Host "  ⚠ $msg" -ForegroundColor Yellow }

# ─── 1. Pre-flight checks ─────────────────────────────────────────────────────
Write-Step "Pre-flight checks"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) is not installed. Install from https://aka.ms/installazurecli"
}

$account = az account show 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Not logged in to Azure CLI. Run 'az login' first."
}
$accountObj = $account | ConvertFrom-Json
Write-OK "Logged in as: $($accountObj.user.name)"
Write-OK "Subscription : $($accountObj.id) ($($accountObj.name))"

# Auto-detect Entra admin from current logged-in user if not supplied
if ([string]::IsNullOrEmpty($SqlEntraAdminObjectId)) {
    try {
        $currentUser = az ad signed-in-user show 2>$null | ConvertFrom-Json
        if ($currentUser -and $currentUser.id) {
            $SqlEntraAdminObjectId = $currentUser.id
            $SqlEntraAdminLogin    = $currentUser.userPrincipalName
            Write-OK "Entra SQL admin set to current user: $SqlEntraAdminLogin"
        }
    } catch {
        throw "Could not detect current Entra user. Provide -SqlEntraAdminObjectId and -SqlEntraAdminLogin."
    }
}

# ─── 2. Create resource group ─────────────────────────────────────────────────
Write-Step "Ensuring resource group '$ResourceGroup' exists in '$Location'"
az group create --name $ResourceGroup --location $Location --output none
Write-OK "Resource group ready."

# ─── 3. Deploy Bicep template ─────────────────────────────────────────────────
Write-Step "Deploying Bicep template (this may take 5-10 minutes)..."

$deploymentName = "contoso-uni-infra-$(Get-Date -Format 'yyyyMMddHHmm')"

$deployOutput = az deployment group create `
    --name $deploymentName `
    --resource-group $ResourceGroup `
    --template-file "$ScriptDir\main.bicep" `
    --parameters `
        "appName=$AppName" `
        "environmentName=$EnvironmentName" `
        "location=$Location" `
        "sqlEntraAdminObjectId=$SqlEntraAdminObjectId" `
        "sqlEntraAdminLogin=$SqlEntraAdminLogin" `
    --output json 2>&1

if ($LASTEXITCODE -ne 0) {
    throw "Bicep deployment failed.`n$deployOutput"
}

$deployment = $deployOutput | ConvertFrom-Json
$outputs    = $deployment.properties.outputs
Write-OK "Deployment '$deploymentName' completed successfully."

# ─── 4. Extract outputs ───────────────────────────────────────────────────────
Write-Step "Extracting deployment outputs"

$appServiceName        = $outputs.appServiceName.value
$appServiceHostName    = $outputs.appServiceHostName.value
$appServicePrincipalId = $outputs.appServicePrincipalId.value
$sqlServerName         = $outputs.sqlServerName.value
$sqlServerFqdn         = $outputs.sqlServerFqdn.value
$sqlDatabaseName       = $outputs.sqlDatabaseName.value
$sbNamespaceName       = $outputs.serviceBusNamespaceName.value
$sbNamespaceFqdn       = $outputs.serviceBusNamespaceFqdn.value
$sbQueueName           = $outputs.serviceBusQueueName.value
$storageAccountName    = $outputs.storageAccountName.value
$storageAccountUri     = $outputs.storageAccountUri.value
$blobContainerName     = $outputs.blobContainerName.value
$appConfigName         = $outputs.appConfigName.value
$appConfigEndpoint     = $outputs.appConfigEndpoint.value

Write-OK "App Service       : $appServiceName  →  https://$appServiceHostName"
Write-OK "SQL Server        : $sqlServerFqdn"
Write-OK "Service Bus       : $sbNamespaceFqdn"
Write-OK "Storage Account   : $storageAccountName  →  $storageAccountUri"
Write-OK "App Configuration : $appConfigEndpoint"

# ─── 5. Populate Azure App Configuration ─────────────────────────────────────
Write-Step "Populating Azure App Configuration with key-values"

$configMigrationFile = Join-Path $PSScriptRoot '..' '.azure' 'configuration-migration.json'
if (Test-Path $configMigrationFile) {
    $configMigration = Get-Content $configMigrationFile | ConvertFrom-Json

    foreach ($kv in $configMigration.keyValues) {
        $value = $kv.value
        $value = $value -replace [regex]::Escape('${SERVICE_BUS_NAMESPACE}.servicebus.windows.net'), $sbNamespaceFqdn
        $value = $value -replace [regex]::Escape('https://<YOUR_STORAGE_ACCOUNT_NAME>.blob.core.windows.net'), $storageAccountUri.TrimEnd('/')
        $value = $value -replace [regex]::Escape('<YOUR_SERVER>.database.windows.net'), $sqlServerFqdn

        if ($kv.key -eq 'ConnectionStrings:DefaultConnection') {
            $value = "Server=tcp:${sqlServerFqdn};Database=${sqlDatabaseName};Authentication=Active Directory Default;TrustServerCertificate=True"
        }

        az appconfig kv set `
            --name $appConfigName `
            --key $kv.key `
            --value $value `
            --yes `
            --output none 2>$null

        Write-OK "  Set: $($kv.key)"
    }
    Write-OK "App Configuration populated."
} else {
    Write-Warn "configuration-migration.json not found at $configMigrationFile. Skipping App Config population."
}

# ─── 6. SQL Managed Identity setup instructions ───────────────────────────────
Write-Step "SQL Managed Identity setup (MANUAL STEP)"
Write-Host @"
  Connect to '$sqlDatabaseName' on '$sqlServerFqdn' using your Entra admin credentials, then run:

  CREATE USER [$appServiceName] FROM EXTERNAL PROVIDER;
  ALTER ROLE db_datareader ADD MEMBER [$appServiceName];
  ALTER ROLE db_datawriter ADD MEMBER [$appServiceName];
  ALTER ROLE db_ddladmin   ADD MEMBER [$appServiceName];
"@ -ForegroundColor Yellow

# ─── 7. Generate infra-config.md ─────────────────────────────────────────────
Write-Step "Generating infra-config.md"

$subscriptionId = $accountObj.id

$infraConfigContent = @"
# Azure Resources Config

## Environment Info

| Property | Value |
|----------|-------|
| Subscription ID | ``$subscriptionId`` |
| Resource Group | ``$ResourceGroup`` |
| Location | ``$Location`` |

## Resource List

| Resource Type | Name | Region | Config Details |
|---------------|------|--------|----------------|
| App Service Plan | ``asp-$AppName-$EnvironmentName`` | $Location | Linux, B2 Basic tier |
| App Service | ``$appServiceName`` | $Location | FQDN: $appServiceHostName, Managed Identity Client ID: $appServicePrincipalId |
| Azure SQL Server | ``$sqlServerName`` | $Location | FQDN: $sqlServerFqdn |
| Azure SQL Database | ``$sqlDatabaseName`` | $Location | Server: $sqlServerFqdn, DB: $sqlDatabaseName |
| Service Bus Namespace | ``$sbNamespaceName`` | $Location | FQDN: $sbNamespaceFqdn |
| Service Bus Queue | ``$sbQueueName`` | $Location | Namespace: $sbNamespaceName |
| Storage Account | ``$storageAccountName`` | $Location | Blob URI: $storageAccountUri |
| Blob Container | ``$blobContainerName`` | $Location | Storage: $storageAccountName |
| App Configuration | ``$appConfigName`` | $Location | Endpoint: $appConfigEndpoint |
"@

$infraConfigContent | Out-File -FilePath "$ScriptDir\infra-config.md" -Encoding utf8 -Force
Write-OK "infra-config.md written."

# ─── Done ─────────────────────────────────────────────────────────────────────
Write-Host "`n✅ Infrastructure provisioning complete!" -ForegroundColor Green
Write-Host "   App URL: https://$appServiceHostName" -ForegroundColor Green

[CmdletBinding()]
param(
    [string]$ResourceGroup    = 'app-mod-cli-full-uni',
    [string]$Location         = 'swedencentral',
    [string]$AppName          = 'contoso-uni',
    [string]$EnvironmentName  = 'prod',
    [string]$SqlAdminLogin    = 'sqladmin',
    [string]$SqlAdminPassword = $env:SQLPASSWORD,
    [string]$SqlEntraAdminObjectId = '',
    [string]$SqlEntraAdminLogin    = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir = $PSScriptRoot

# ─── Helpers ──────────────────────────────────────────────────────────────────
function Write-Step([string]$msg) { Write-Host "`n► $msg" -ForegroundColor Cyan }
function Write-OK([string]$msg)   { Write-Host "  ✔ $msg" -ForegroundColor Green }
function Write-Warn([string]$msg) { Write-Host "  ⚠ $msg" -ForegroundColor Yellow }

# ─── 1. Pre-flight checks ─────────────────────────────────────────────────────
Write-Step "Pre-flight checks"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) is not installed. Install from https://aka.ms/installazurecli"
}

$account = az account show 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Not logged in to Azure CLI. Run 'az login' first."
}
$accountObj = $account | ConvertFrom-Json
Write-OK "Logged in as: $($accountObj.user.name)"
Write-OK "Subscription : $($accountObj.id) ($($accountObj.name))"

# Prompt for SQL password if not provided
if ([string]::IsNullOrEmpty($SqlAdminPassword)) {
    $securePass = Read-Host "Enter SQL administrator password" -AsSecureString
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePass)
    $SqlAdminPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
}

# Auto-detect Entra admin from current logged-in user if not supplied
if ([string]::IsNullOrEmpty($SqlEntraAdminObjectId)) {
    try {
        $currentUser = az ad signed-in-user show 2>$null | ConvertFrom-Json
        if ($currentUser -and $currentUser.id) {
            $SqlEntraAdminObjectId = $currentUser.id
            $SqlEntraAdminLogin    = $currentUser.userPrincipalName
            Write-OK "Entra SQL admin set to current user: $SqlEntraAdminLogin"
        }
    } catch {
        Write-Warn "Could not detect current Entra user. SQL Entra admin will not be configured."
    }
}

# ─── 2. Create resource group ─────────────────────────────────────────────────
Write-Step "Ensuring resource group '$ResourceGroup' exists in '$Location'"
az group create --name $ResourceGroup --location $Location --output none
Write-OK "Resource group ready."

# ─── 3. Deploy Bicep template ─────────────────────────────────────────────────
Write-Step "Deploying Bicep template (this may take 5-10 minutes)..."

$deploymentName = "contoso-uni-infra-$(Get-Date -Format 'yyyyMMddHHmm')"

$deployArgs = @(
    'deployment', 'group', 'create',
    '--name',           $deploymentName,
    '--resource-group', $ResourceGroup,
    '--template-file',  "$ScriptDir\main.bicep",
    '--parameters',
        "appName=$AppName",
        "environmentName=$EnvironmentName",
        "location=$Location",
        "sqlAdminLogin=$SqlAdminLogin",
        "sqlAdminPassword=$SqlAdminPassword"
)

if (-not [string]::IsNullOrEmpty($SqlEntraAdminObjectId)) {
    $deployArgs += "sqlEntraAdminObjectId=$SqlEntraAdminObjectId"
    $deployArgs += "sqlEntraAdminLogin=$SqlEntraAdminLogin"
}

$deployArgs += '--output'
$deployArgs += 'json'

$deployOutput = az @deployArgs
if ($LASTEXITCODE -ne 0) {
    throw "Bicep deployment failed. Check the Azure portal for details."
}

$deployment = $deployOutput | ConvertFrom-Json
$outputs    = $deployment.properties.outputs
Write-OK "Deployment '$deploymentName' completed successfully."

# ─── 4. Extract outputs ───────────────────────────────────────────────────────
Write-Step "Extracting deployment outputs"

$appServiceName        = $outputs.appServiceName.value
$appServiceHostName    = $outputs.appServiceHostName.value
$appServicePrincipalId = $outputs.appServicePrincipalId.value
$sqlServerName         = $outputs.sqlServerName.value
$sqlServerFqdn         = $outputs.sqlServerFqdn.value
$sqlDatabaseName       = $outputs.sqlDatabaseName.value
$sbNamespaceName       = $outputs.serviceBusNamespaceName.value
$sbNamespaceFqdn       = $outputs.serviceBusNamespaceFqdn.value
$sbQueueName           = $outputs.serviceBusQueueName.value
$storageAccountName    = $outputs.storageAccountName.value
$storageAccountUri     = $outputs.storageAccountUri.value
$blobContainerName     = $outputs.blobContainerName.value
$appConfigName         = $outputs.appConfigName.value
$appConfigEndpoint     = $outputs.appConfigEndpoint.value

Write-OK "App Service       : $appServiceName  →  https://$appServiceHostName"
Write-OK "SQL Server        : $sqlServerFqdn"
Write-OK "Service Bus       : $sbNamespaceFqdn"
Write-OK "Storage Account   : $storageAccountName  →  $storageAccountUri"
Write-OK "App Configuration : $appConfigEndpoint"

# ─── 5. Populate Azure App Configuration ─────────────────────────────────────
Write-Step "Populating Azure App Configuration with key-values"

$configMigrationFile = Join-Path $PSScriptRoot '..' '.azure' 'configuration-migration.json'
if (Test-Path $configMigrationFile) {
    $configMigration = Get-Content $configMigrationFile | ConvertFrom-Json

    # Map placeholder values to real provisioned values
    $replacements = @{
        '${SERVICE_BUS_NAMESPACE}.servicebus.windows.net' = $sbNamespaceFqdn
        "https://<YOUR_STORAGE_ACCOUNT_NAME>.blob.core.windows.net" = $storageAccountUri.TrimEnd('/')
        "<YOUR_SERVER>.database.windows.net" = $sqlServerFqdn
    }

    foreach ($kv in $configMigration.keyValues) {
        $value = $kv.value
        foreach ($placeholder in $replacements.Keys) {
            $value = $value -replace [regex]::Escape($placeholder), $replacements[$placeholder]
        }
        # Also fix the ConnectionStrings value with correct DB name
        if ($kv.key -eq 'ConnectionStrings:DefaultConnection') {
            $value = "Server=tcp:${sqlServerFqdn};Database=${sqlDatabaseName};Authentication=Active Directory Default;TrustServerCertificate=True"
        }

        az appconfig kv set `
            --name $appConfigName `
            --key $kv.key `
            --value $value `
            --yes `
            --output none 2>$null

        Write-OK "  Set: $($kv.key)"
    }
    Write-OK "App Configuration populated."
} else {
    Write-Warn "configuration-migration.json not found at $configMigrationFile. Skipping App Config population."
}

# ─── 6. SQL Managed Identity setup instructions ───────────────────────────────
Write-Step "SQL Managed Identity setup"
Write-Warn "MANUAL STEP REQUIRED: Run the following T-SQL against '$sqlDatabaseName' on '$sqlServerFqdn'"
Write-Host @"
  -- Connect using your Entra admin credentials, then run:
  CREATE USER [$appServiceName] FROM EXTERNAL PROVIDER;
  ALTER ROLE db_datareader ADD MEMBER [$appServiceName];
  ALTER ROLE db_datawriter ADD MEMBER [$appServiceName];
  ALTER ROLE db_ddladmin   ADD MEMBER [$appServiceName];
"@ -ForegroundColor Yellow

# ─── 7. Generate infra-config.md ─────────────────────────────────────────────
Write-Step "Generating infra-config.md"

$subscriptionId = $accountObj.id

$infraConfigContent = @"
# Azure Resources Config

## Environment Info

| Property | Value |
|----------|-------|
| Subscription ID | ``$subscriptionId`` |
| Resource Group | ``$ResourceGroup`` |
| Location | ``$Location`` |

## Resource List

| Resource Type | Name | Region | Config Details |
|---------------|------|--------|----------------|
| App Service Plan | ``asp-$AppName-$EnvironmentName`` | $Location | Linux, B2 Basic tier |
| App Service | ``$appServiceName`` | $Location | FQDN: $appServiceHostName, Managed Identity: $appServicePrincipalId |
| Azure SQL Server | ``$sqlServerName`` | $Location | FQDN: $sqlServerFqdn |
| Azure SQL Database | ``$sqlDatabaseName`` | $Location | Server: $sqlServerFqdn, DB: $sqlDatabaseName |
| Service Bus Namespace | ``$sbNamespaceName`` | $Location | FQDN: $sbNamespaceFqdn |
| Service Bus Queue | ``$sbQueueName`` | $Location | Namespace: $sbNamespaceName |
| Storage Account | ``$storageAccountName`` | $Location | Blob URI: $storageAccountUri |
| Blob Container | ``$blobContainerName`` | $Location | Storage: $storageAccountName |
| App Configuration | ``$appConfigName`` | $Location | Endpoint: $appConfigEndpoint |
"@

$infraConfigContent | Out-File -FilePath "$ScriptDir\infra-config.md" -Encoding utf8 -Force
Write-OK "infra-config.md written to $ScriptDir\infra-config.md"

# ─── Done ─────────────────────────────────────────────────────────────────────
Write-Host "`n✅ Infrastructure provisioning complete!" -ForegroundColor Green
Write-Host "   App URL: https://$appServiceHostName" -ForegroundColor Green
