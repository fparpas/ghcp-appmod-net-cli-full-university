# Deployment Progress

## ContosoUniversity → Azure App Service

| Step | Status | Notes |
|------|--------|-------|
| Env setup (AZ CLI, subscription, extension) | ✅ Completed | AZ CLI 2.86.0, serviceconnector-passwordless 3.3.6, subscription 94bc45db set |
| Check Azure resources existence | ✅ Completed | All 8 resources verified: App Service, SQL DB, Service Bus, Blob Storage, App Config |
| Configure App Service settings | ✅ Completed | 8 app settings configured including `AZURE_APP_CONFIGURATION_ENDPOINT`, `WEBSITE_RUN_FROM_PACKAGE=1`, SQL connection string |
| Assign RBAC roles to Managed Identity | ✅ Completed | Service Bus Data Owner, Storage Blob Data Contributor, App Configuration Data Reader assigned |
| Add MI as SQL Database user | ✅ Completed | `app-contoso-uni-2psof2l5` EXTERNAL_USER with db_datareader, db_datawriter, db_ddladmin |
| Seed Azure App Configuration | ✅ Completed | 8 key-values seeded with real infrastructure values |
| Fix SqlClient v7 AAD auth | ✅ Fixed | Added `Microsoft.Data.SqlClient.Extensions.Azure` + `ActiveDirectoryAuthenticationProvider.SetProvider()` in Program.cs |
| Build & publish .NET 10 app | ✅ Completed | `dotnet publish -c Release -r linux-x64 --self-contained false` → 8.3 MB ZIP |
| ZIP deploy via az webapp deploy | ✅ Completed | RuntimeSuccessful, 1/1 instances, started in 65s |
| Deployment validation (logs + browser) | ✅ Completed | HTTP 200, `<title>Home Page - Contoso University</title>` confirmed |
| Summarize result | ✅ Completed | See deployment-summary.md |
