# Azure Resources Config

## Environment Info

| Property | Value |
|----------|-------|
| Subscription ID | `94bc45db-2c21-4a0e-a881-762c4d44751a` |
| Resource Group | `rg-contosouniv-dev` |
| Location | `centralus` |
| Tenant ID | `0f64c39d-2e8d-4339-909a-12bd5577ba8e` |
| Unique Suffix | `aoftol` |

## Resource List

| Resource Type | Name | Region | Config Details |
|---------------|------|---------|----------------|
| Azure App Service Plan | `asp-contosouniv-dev` | centralus | SKU: F1 Linux |
| Azure Web App | `app-contosouniv-dev-aoftol` | centralus | FQDN: app-contosouniv-dev-aoftol.azurewebsites.net; Runtime: DOTNETCORE\|10.0; System-Assigned MI: 42973e4a-0807-44f3-84d8-127c982dd2d9 |
| Azure SQL Server | `sql-contosouniv-dev-aoftol` | centralus | FQDN: sql-contosouniv-dev-aoftol.database.windows.net; Azure AD-only auth |
| Azure SQL Database | `sqldb-contosouniv-dev` | centralus | Server: sql-contosouniv-dev-aoftol.database.windows.net |
| Azure Service Bus Namespace | `sb-contosouniv-dev-aoftol` | centralus | FQDN: sb-contosouniv-dev-aoftol.servicebus.windows.net; Queue: notifications |
| Azure Storage Account | `stcontosounivdevaoftol` | centralus | Blob endpoint: https://stcontosounivdevaoftol.blob.core.windows.net; Container: teaching-materials |
| Azure App Configuration | `appcs-contosouniv-dev-aoftol` | centralus | Endpoint: https://appcs-contosouniv-dev-aoftol.azconfig.io |

## Managed Identity RBAC

| Role | Scope | Assigned To |
|------|-------|-------------|
| App Configuration Data Reader | appcs-contosouniv-dev-aoftol | app-contosouniv-dev-aoftol (MI) |
| Azure Service Bus Data Sender | sb-contosouniv-dev-aoftol | app-contosouniv-dev-aoftol (MI) |
| Azure Service Bus Data Receiver | sb-contosouniv-dev-aoftol | app-contosouniv-dev-aoftol (MI) |
| Storage Blob Data Contributor | stcontosounivdevaoftol | app-contosouniv-dev-aoftol (MI) |
| db_owner | sqldb-contosouniv-dev | app-contosouniv-dev-aoftol (MI, external user) |
