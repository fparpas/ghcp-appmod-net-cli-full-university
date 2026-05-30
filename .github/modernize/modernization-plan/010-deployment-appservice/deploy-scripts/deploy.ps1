#!/usr/bin/env pwsh
# deploy.ps1 — Deploy ContosoUniversity to Azure App Service (app-contoso-uni-2psof2l5)
# Subscription: 94bc45db-2c21-4a0e-a881-762c4d44751a
# Resource Group: app-mod-cli-full-uni

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$REPO_ROOT    = Resolve-Path "$PSScriptRoot\..\..\..\..\..\.."
$SUBSCRIPTION = "94bc45db-2c21-4a0e-a881-762c4d44751a"
$RG           = "app-mod-cli-full-uni"
$APP_NAME     = "app-contoso-uni-2psof2l5"
$PUBLISH_DIR  = Join-Path $REPO_ROOT "publish"
$ZIP_PATH     = Join-Path $REPO_ROOT "contosouniversity.zip"
$PROJECT_FILE = Join-Path $REPO_ROOT "ContosoUniversity.csproj"

Write-Host "`n=== Step 1: Set subscription ===" -ForegroundColor Cyan
az account set --subscription $SUBSCRIPTION

Write-Host "`n=== Step 2: Build & publish .NET 10 app ===" -ForegroundColor Cyan
if (Test-Path $PUBLISH_DIR) { Remove-Item -Recurse -Force $PUBLISH_DIR }
dotnet publish $PROJECT_FILE -c Release -r linux-x64 --self-contained false -o $PUBLISH_DIR

Write-Host "`n=== Step 3: Create ZIP package ===" -ForegroundColor Cyan
if (Test-Path $ZIP_PATH) { Remove-Item -Force $ZIP_PATH }
Compress-Archive -Path "$PUBLISH_DIR\*" -DestinationPath $ZIP_PATH -Force
$zipSize = (Get-Item $ZIP_PATH).Length / 1MB
Write-Host "ZIP created: $ZIP_PATH ($([math]::Round($zipSize,1)) MB)"

Write-Host "`n=== Step 4: Deploy to Azure App Service ===" -ForegroundColor Cyan
az webapp deploy `
    --name $APP_NAME `
    --resource-group $RG `
    --src-path $ZIP_PATH `
    --type zip `
    --async false

Write-Host "`n=== Step 5: Restart App Service ===" -ForegroundColor Cyan
az webapp restart --name $APP_NAME --resource-group $RG

Write-Host "`n=== Deployment complete ===" -ForegroundColor Green
Write-Host "App URL: https://$APP_NAME.azurewebsites.net"
