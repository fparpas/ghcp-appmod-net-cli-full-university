# Deployment Summary

## Task: 010-deployment-appservice
**Status: ✅ SUCCEEDED**  
**Date**: 2026-05-30  
**Duration**: ~65 seconds site startup after final deploy

---

## Application

| Property | Value |
|----------|-------|
| Application | ContosoUniversity |
| Framework | .NET 10 (net10.0), ASP.NET Core MVC |
| Deployed URL | https://app-contoso-uni-2psof2l5.azurewebsites.net |
| HTTP Status | **200 OK** ✅ |
| Page Verified | `<title>Home Page - Contoso University</title>` ✅ |

---

## Azure Resources Used

| Resource | Name | Status |
|----------|------|--------|
| App Service | `app-contoso-uni-2psof2l5` | ✅ Running |
| App Service Plan | `asp-contoso-uni-prod` | ✅ Linux B2 |
| Azure SQL Database | `ContosoUniversityDB` on `sql-contoso-uni-2psof2l5` | ✅ Online |
| Service Bus | `sb-contoso-uni-2psof2l5` / queue `contoso-university-notifications` | ✅ Active |
| Blob Storage | `stcontosouni2psof2l5` / container `teaching-materials` | ✅ Active |
| App Configuration | `appcs-contoso-uni-2psof2l5` | ✅ Active |
| Subscription | `94bc45db-2c21-4a0e-a881-762c4d44751a` | ✅ |
| Resource Group | `app-mod-cli-full-uni` | ✅ |

---

## Deployment Method

- **Tool**: Azure CLI `az webapp deploy --type zip`
- **Package**: `contosouniversity.zip` (8.3 MB, framework-dependent Linux x64)
- **Build**: `dotnet publish -c Release -r linux-x64 --self-contained false`
- **Runtime**: `DOTNETCORE|10.0` on Linux App Service
- **Startup**: Auto-detected by Oryx → `dotnet ContosoUniversity.dll`

---

## Configuration Applied

### App Settings
| Setting | Value |
|---------|-------|
| `AZURE_APP_CONFIGURATION_ENDPOINT` | `https://appcs-contoso-uni-2psof2l5.azconfig.io` |
| `AzureServiceBus__FullyQualifiedNamespace` | `sb-contoso-uni-2psof2l5.servicebus.windows.net` |
| `AzureServiceBus__QueueName` | `contoso-university-notifications` |
| `Storage__ServiceUri` | `https://stcontosouni2psof2l5.blob.core.windows.net/` |
| `Storage__ContainerName` | `teaching-materials` |
| `ConnectionStrings__DefaultConnection` | `Server=tcp:sql-contoso-uni-2psof2l5...;Authentication=Active Directory Default` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `WEBSITE_RUN_FROM_PACKAGE` | `1` |

### RBAC Roles (Managed Identity: `fbec5cdf-5127-49de-868d-73f32e5aeb6a`)
| Role | Scope |
|------|-------|
| Azure Service Bus Data Owner | `sb-contoso-uni-2psof2l5` namespace |
| Storage Blob Data Contributor | `stcontosouni2psof2l5` storage account |
| App Configuration Data Reader | `appcs-contoso-uni-2psof2l5` store |

### SQL Database User
- User `app-contoso-uni-2psof2l5` created as `EXTERNAL_USER`
- Roles: `db_datareader`, `db_datawriter`, `db_ddladmin`

---

## Issues Resolved During Deployment

| Issue | Root Cause | Fix Applied |
|-------|------------|-------------|
| `ActiveDirectoryDefault` auth provider not found | `Microsoft.Data.SqlClient` v7.x moved AAD auth to separate package | Added `Microsoft.Data.SqlClient.Extensions.Azure` v1.0.0 and registered `ActiveDirectoryAuthenticationProvider` |
| `Azure.Identity` version conflict | Extensions.Azure requires ≥ 1.18.0 | Upgraded `Azure.Identity` from 1.14.0 → 1.18.0 |
| App serving default welcome page | `DOTNETCORE` vs `DOTNET` runtime mismatch during config changes | Set linuxFxVersion to `DOTNETCORE|10.0`, re-deployed with `WEBSITE_RUN_FROM_PACKAGE=1` |

---

## Code Changes Made

| File | Change |
|------|--------|
| `ContosoUniversity.csproj` | Added `Microsoft.Data.SqlClient.Extensions.Azure` v1.0.0, upgraded `Azure.Identity` to 1.18.0 |
| `Program.cs` | Added `SqlAuthenticationProvider.SetProvider(ActiveDirectoryDefault, new ActiveDirectoryAuthenticationProvider())` |

---

## Post-Deployment Notes

> The Entra ID (Microsoft Identity Web) authentication is configured but requires valid `AzureAd:TenantId`, `AzureAd:ClientId`, and `AzureAd:Domain` values to be set in Azure App Configuration to enable user sign-in. These values depend on an Entra ID app registration for the production environment.
