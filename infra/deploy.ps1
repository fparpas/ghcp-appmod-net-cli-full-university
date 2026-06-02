# deploy.ps1 — Deploy ContosoUniversity infrastructure to Azure
# Usage:
#   .\infra\deploy.ps1 -ResourceGroupName rg-contosouniv-dev `
#                      -SubscriptionId <id> `
#                      -SqlAdminPassword <pwd> `
#                      -AadAdminLogin <login> `
#                      -AadAdminObjectId <oid> `
#                      -AadTenantId <tid>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $false)]
    [string]$Location = 'eastus',

    [Parameter(Mandatory = $true)]
    [string]$SubscriptionId,

    [Parameter(Mandatory = $true)]
    [string]$SqlAdminPassword,

    [Parameter(Mandatory = $true)]
    [string]$AadAdminLogin,

    [Parameter(Mandatory = $true)]
    [string]$AadAdminObjectId,

    [Parameter(Mandatory = $true)]
    [string]$AadTenantId,

    [Parameter(Mandatory = $false)]
    [string]$EnvironmentName = 'dev',

    [Parameter(Mandatory = $false)]
    [string]$AppName = 'contosouniv'
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== ContosoUniversity Infrastructure Deployment ===" -ForegroundColor Cyan
Write-Host "Resource Group : $ResourceGroupName"
Write-Host "Location       : $Location"
Write-Host "Subscription   : $SubscriptionId"
Write-Host "Environment    : $EnvironmentName"
Write-Host ""

# Set subscription
Write-Host ">> Setting subscription..." -ForegroundColor Yellow
az account set --subscription $SubscriptionId
if ($LASTEXITCODE -ne 0) { throw "Failed to set subscription." }

# Create resource group
Write-Host ">> Creating resource group '$ResourceGroupName' in '$Location'..." -ForegroundColor Yellow
az group create `
    --name $ResourceGroupName `
    --location $Location `
    --tags "application=contosouniversity" "environment=$EnvironmentName" "managedBy=bicep"
if ($LASTEXITCODE -ne 0) { throw "Failed to create resource group." }

# Deploy Bicep template
Write-Host ">> Deploying Bicep template..." -ForegroundColor Yellow
$DeployOutput = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file "$ScriptDir\main.bicep" `
    --parameters "$ScriptDir\parameters.json" `
    --parameters `
        environmentName=$EnvironmentName `
        location=$Location `
        appName=$AppName `
        sqlAdminPassword=$SqlAdminPassword `
        aadAdminLogin=$AadAdminLogin `
        aadAdminObjectId=$AadAdminObjectId `
        aadTenantId=$AadTenantId `
    --output json | ConvertFrom-Json

if ($LASTEXITCODE -ne 0) { throw "Bicep deployment failed." }

Write-Host ""
Write-Host "=== Deployment Outputs ===" -ForegroundColor Cyan
$outputs = $DeployOutput.properties.outputs
foreach ($key in $outputs.PSObject.Properties.Name) {
    Write-Host "  $key : $($outputs.$key.value)"
}

$webAppName     = $outputs.webAppName.value
$sqlServerFqdn  = $outputs.sqlServerFqdn.value

Write-Host ""
Write-Host "=== Post-Deployment: SQL Managed Identity Setup ===" -ForegroundColor Cyan
Write-Host "Run the following SQL script against '$sqlServerFqdn' to grant managed identity access:"
Write-Host ""
Write-Host "  CREATE USER [$webAppName] FROM EXTERNAL PROVIDER;" -ForegroundColor Green
Write-Host "  ALTER ROLE db_owner ADD MEMBER [$webAppName];"     -ForegroundColor Green
Write-Host ""
Write-Host "You can run it via Azure Portal Query Editor or sqlcmd with AAD authentication."
Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Cyan
