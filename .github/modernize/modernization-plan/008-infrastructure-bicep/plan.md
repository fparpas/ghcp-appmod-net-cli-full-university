# Task 008: Infrastructure Bicep Generation

## Objective
Generate Bicep IaC files to provision all Azure resources required by ContosoUniversity.

## Resources
- Azure App Service Plan (B2) + Web App (.NET 10, Linux)
- Azure SQL Server + Database (GeneralPurpose Serverless)
- Azure Service Bus Namespace (Standard) + Queue: notifications
- Azure Storage Account (Standard_LRS) + Container: teaching-materials
- Azure App Configuration (Free)
- Managed Identity role assignments (Service Bus Sender/Receiver, Blob Contributor, App Config Reader)

## Files Generated
- `infra/main.bicep`
- `infra/parameters.json`
- `infra/modules/appservice.bicep`
- `infra/modules/sql.bicep`
- `infra/modules/servicebus.bicep`
- `infra/modules/storage.bicep`
- `infra/modules/appconfiguration.bicep`
- `infra/modules/roleassignments.bicep`
- `infra/deploy.sh`
- `infra/deploy.ps1`
- `infra/README.md`
- `infra/infra-config.md`
- `infra/compliance.md`

## Execution Steps
1. Install Azure CLI and Bicep CLI (`az bicep install`)
2. Login: `az login`
3. Set subscription: `az account set --subscription <subscriptionId>`
4. Run: `./infra/deploy.ps1 -ResourceGroupName rg-contosouniv-dev -SubscriptionId <id> -SqlAdminPassword <pwd> -AadAdminLogin <login> -AadAdminObjectId <oid> -AadTenantId <tid>` (Windows)
   Or: `./infra/deploy.sh` (Linux/macOS)
5. Post-deploy: run SQL script to add web app managed identity as database user
6. Seed Azure App Configuration with values from `.azure/configuration-migration.json`

## Status
Provision: false (files generated, not yet provisioned)
