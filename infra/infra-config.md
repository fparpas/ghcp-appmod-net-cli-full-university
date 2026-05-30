# Azure Resources Config

## Environment Info

| Property | Value |
|----------|-------|
| Subscription ID | `94bc45db-2c21-4a0e-a881-762c4d44751a` |
| Resource Group | `app-mod-cli-full-uni` |
| Location | `swedencentral` |

## Resource List

| Resource Type | Name | Region | Config Details |
|---------------|------|--------|----------------|
| App Service Plan | `asp-contoso-uni-prod` | swedencentral | Linux, B2 Basic tier |
| App Service | `app-contoso-uni-2psof2l5` | swedencentral | FQDN: app-contoso-uni-2psof2l5.azurewebsites.net, Managed Identity Client ID: fbec5cdf-5127-49de-868d-73f32e5aeb6a |
| Azure SQL Server | `sql-contoso-uni-2psof2l5` | swedencentral | FQDN: sql-contoso-uni-2psof2l5.database.windows.net |
| Azure SQL Database | `ContosoUniversityDB` | swedencentral | Server: sql-contoso-uni-2psof2l5.database.windows.net, DB: ContosoUniversityDB |
| Service Bus Namespace | `sb-contoso-uni-2psof2l5` | swedencentral | FQDN: sb-contoso-uni-2psof2l5.servicebus.windows.net |
| Service Bus Queue | `contoso-university-notifications` | swedencentral | Namespace: sb-contoso-uni-2psof2l5 |
| Storage Account | `stcontosouni2psof2l5` | swedencentral | Blob URI: https://stcontosouni2psof2l5.blob.core.windows.net/ |
| Blob Container | `teaching-materials` | swedencentral | Storage: stcontosouni2psof2l5 |
| App Configuration | `appcs-contoso-uni-2psof2l5` | swedencentral | Endpoint: https://appcs-contoso-uni-2psof2l5.azconfig.io |
