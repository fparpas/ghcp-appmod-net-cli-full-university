# Azure Resources Config

## Environment Info

| Property | Value |
|----------|-------|
| Subscription ID | `<to-be-filled-after-provisioning>` |
| Resource Group | `rg-contosouniv-dev` |
| Location | `eastus` |

## Resource List

| Resource Type | Name | Region | Config Details |
|---------------|------|---------|----------------|
| Azure App Service Plan | `asp-contosouniv-dev` | eastus | SKU: B2 Linux |
| Azure Web App | `app-contosouniv-dev-<suffix>` | eastus | FQDN: app-contosouniv-dev-<suffix>.azurewebsites.net |
| Azure SQL Server | `sql-contosouniv-dev-<suffix>` | eastus | FQDN: sql-contosouniv-dev-<suffix>.database.windows.net |
| Azure SQL Database | `sqldb-contosouniv-dev` | eastus | Server: sql-contosouniv-dev-<suffix>.database.windows.net |
| Azure Service Bus Namespace | `sb-contosouniv-dev-<suffix>` | eastus | Endpoint: sb-contosouniv-dev-<suffix>.servicebus.windows.net |
| Azure Storage Account | `stcontosounivdev<suffix>` | eastus | Blob endpoint: https://stcontosounivdev<suffix>.blob.core.windows.net |
| Azure App Configuration | `appcs-contosouniv-dev-<suffix>` | eastus | Endpoint: https://appcs-contosouniv-dev-<suffix>.azconfig.io |
