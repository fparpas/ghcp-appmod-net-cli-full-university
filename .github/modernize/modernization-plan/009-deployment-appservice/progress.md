# Deployment Progress — 009-deployment-appservice

## Status: ✅ COMPLETED

**App URL:** https://app-contosouniv-dev-aoftol.azurewebsites.net

---

## Steps

- ✅ Plan generated (`plan.md` created)
- ✅ Step 1 — Env Setup for AzCLI (AZ CLI 2.86.0, logged in as phanisparpas@microsoft.com, sub: MS Parpas 2)
- ✅ Step 2 — Azure Resources Provisioned via Bicep (centralus)
  - ✅ App Service Plan: `asp-contosouniv-dev` (F1 Linux, centralus)
  - ✅ Web App: `app-contosouniv-dev-aoftol` (FQDN: app-contosouniv-dev-aoftol.azurewebsites.net)
  - ✅ SQL Server: `sql-contosouniv-dev-aoftol` (Azure AD-only auth, centralus)
  - ✅ SQL Database: `sqldb-contosouniv-dev`
  - ✅ Service Bus: `sb-contosouniv-dev-aoftol` + queue: `notifications`
  - ✅ Storage Account: `stcontosounivdevaoftol` + container: `teaching-materials`
  - ✅ App Configuration: `appcs-contosouniv-dev-aoftol`
  - ✅ Role Assignments (Service Bus Sender/Receiver, Storage Blob Contributor, App Config Reader)
  - Fixed: `azureADOnlyAuthentication: true` (MCAPS policy compliance)
  - Fixed: Service Bus Data Receiver role ID (typo in original Bicep)
- ✅ Step 3 — Build Application (dotnet publish --runtime linux-x64 --self-contained false)
  - Fixed: Missing `bootstrap.css` added to Content/ folder
  - Fixed: Published with `--runtime linux-x64` to avoid `BadImageFormatException` on Linux
- ✅ Step 4 — Seed Azure App Configuration (10 key-values seeded)
- ✅ Step 5 — Configure App Service Settings (7 app settings configured)
- ✅ Step 6 — Deploy Application
  - Fixed: ZIP created with forward-slash paths (Linux Kudu compatibility)
  - Fixed: `linuxFxVersion: 'DOTNETCORE|10.0'` (was DOTNET|10.0)
  - Fixed: Program.cs App Config startup made resilient (try-catch)
  - Fixed: SQL managed identity granted db_owner role
  - Final: linux-x64 package deployed → RuntimeSuccessful
- ✅ Step 7 — Deployment Validation
  - Home page: HTTP 200 ✅
  - About page: HTTP 200 ✅
  - Students: HTTP 200 ✅ (live DB queries)
  - Courses: HTTP 200 ✅ (live DB queries)
  - Instructors: HTTP 200 ✅
  - Departments: HTTP 200 ✅
  - Notifications: HTTP 200 ✅
  - Static files (bootstrap.css, modernizr.js, Site.css): HTTP 200 ✅
- ✅ Step 8 — Result Summarized (see deployment-summary.md)

