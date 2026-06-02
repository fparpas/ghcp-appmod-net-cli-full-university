# Deployment Summary — ContosoUniversity to Azure App Service

## Result: ✅ Successfully Deployed

**Live URL:** https://app-contosouniv-dev-aoftol.azurewebsites.net  
**Deployment Date:** 2026-06-02  
**Target:** Azure App Service (Linux, F1 SKU, .NET 10)

---

## Architecture

```
Browser → HTTPS → Azure App Service (app-contosouniv-dev-aoftol)
                           │
              ┌────────────┼──────────────┐
              ▼            ▼              ▼
       Azure SQL DB   Service Bus   Blob Storage
    (Managed Identity) (MI Auth)    (MI Auth)
              │
       App Configuration
          (MI Auth)
```

---

## Provisioned Azure Resources

| Resource | Name | Location |
|----------|------|----------|
| Resource Group | `rg-contosouniv-dev` | centralus |
| App Service Plan | `asp-contosouniv-dev` | centralus (F1 Linux) |
| Web App | `app-contosouniv-dev-aoftol` | centralus |
| SQL Server | `sql-contosouniv-dev-aoftol` | centralus |
| SQL Database | `sqldb-contosouniv-dev` | centralus |
| Service Bus | `sb-contosouniv-dev-aoftol` | centralus |
| Storage Account | `stcontosounivdevaoftol` | centralus |
| App Configuration | `appcs-contosouniv-dev-aoftol` | centralus |

---

## Security: Managed Identity (Zero-Credential)

The web app uses a System-Assigned Managed Identity for all Azure service connections.  
No passwords, connection string credentials, or shared secrets are stored anywhere.

| Azure Service | Permission | Authentication |
|---------------|-----------|----------------|
| Azure SQL Database | db_owner | Managed Identity |
| Azure Service Bus | Data Sender + Receiver | Managed Identity |
| Azure Blob Storage | Storage Blob Data Contributor | Managed Identity |
| Azure App Configuration | App Configuration Data Reader | Managed Identity |

---

## Application Configuration

App settings are loaded in priority order:
1. **Azure App Configuration** (cloud-first, managed identity auth)
2. **Azure App Service Environment Variables** (fallback, set at deployment)

Key settings configured:
- `ASPNETCORE_ENVIRONMENT` → Development
- `AZURE_APP_CONFIGURATION_ENDPOINT` → https://appcs-contosouniv-dev-aoftol.azconfig.io
- `ConnectionStrings__DefaultConnection` → Azure SQL (MI auth)
- `AzureServiceBus__FullyQualifiedNamespace` → sb-contosouniv-dev-aoftol.servicebus.windows.net
- `AzureServiceBus__QueueName` → notifications
- `Storage__ServiceUri` → https://stcontosounivdevaoftol.blob.core.windows.net/
- `Storage__ContainerName` → teaching-materials

---

## Issues Resolved During Deployment

| # | Issue | Resolution |
|---|-------|------------|
| 1 | MCAPS policy: SQL must have Azure AD-only auth | Set `azureADOnlyAuthentication: true` in sql.bicep; removed SQL admin password |
| 2 | VM quota exceeded for B1/B2 SKU in eastus/eastus2 | Changed region to `centralus`, SKU to F1 (free tier) |
| 3 | App Config soft-delete conflict after RG recreation | Ran `az appconfig purge` to clear soft-deleted store |
| 4 | Wrong Service Bus Data Receiver role ID in Bicep | Fixed GUID in roleassignments.bicep |
| 5 | ZIP created with Windows backslash paths | Rebuilt ZIP using `ZipArchive` with explicit forward-slash normalization |
| 6 | `linuxFxVersion: 'DOTNET\|10.0'` — wrong runtime | Fixed to `DOTNETCORE\|10.0` via ARM REST API PATCH |
| 7 | App Config 403 at startup crashing the app | Wrapped `AddAzureAppConfiguration` in try-catch (optional fallback) |
| 8 | `BadImageFormatException` — Windows native DLLs on Linux | Re-published with `--runtime linux-x64 --self-contained false` |
| 9 | `bootstrap.css` missing from Content/ folder | Downloaded Bootstrap 3.4.1 CSS and added to project |
| 10 | Bicep RBAC assignments not applied (race condition) | Manually assigned all 4 RBAC roles via `az role assignment create` |
| 11 | SQL login failed — MI not in SQL database | Created external user and granted `db_owner` via dotnet SqlClient script |

---

## Known Limitations

- **Entra ID Authentication**: `AzureAd:ClientId` is set to `YOUR_APP_REGISTRATION_CLIENT_ID`. 
  Authenticated routes will fail until a real Azure App Registration is created and the client ID is configured.
- **F1 SKU limitations**: No Always-On, limited CPU/memory. Upgrade to B1+ for production.
- **App Config Keys**: Some keys still reference placeholder values (e.g., ClientId).

---

## Validation Results

| Test | Status |
|------|--------|
| Homepage (`/`) | ✅ HTTP 200 |
| About page (`/Home/About`) | ✅ HTTP 200 |
| Students list (`/Students`) | ✅ HTTP 200, live SQL queries |
| Courses list (`/Courses`) | ✅ HTTP 200, live SQL queries |
| Instructors (`/Instructors`) | ✅ HTTP 200 |
| Departments (`/Departments`) | ✅ HTTP 200 |
| Notifications (`/Notifications`) | ✅ HTTP 200 |
| Bootstrap CSS (`/Content/bootstrap.css`) | ✅ HTTP 200 |
| Modernizr JS (`/Scripts/modernizr-2.6.2.js`) | ✅ HTTP 200 |
| Site CSS (`/Content/Site.css`) | ✅ HTTP 200 |
