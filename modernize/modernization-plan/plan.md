# Modernization Plan: modernization-plan

**Project**: ContosoUniversity

---

## Technical Framework

- **Language**: C# / .NET Framework 4.8
- **Framework**: ASP.NET MVC 5 (System.Web), Entity Framework Core 3.1
- **Build Tool**: MSBuild (non-SDK-style project)
- **Database**: SQL Server (LocalDB via connection string in Web.config), accessed via EF Core 3.1
- **Key Dependencies**: System.Web.Mvc 5.2.9, System.Messaging (MSMQ), Newtonsoft.Json 13.0.3, Microsoft.EntityFrameworkCore.SqlServer 3.1.32, Microsoft.Identity.Client 4.21.1

---

## Overview

This migration modernizes the ContosoUniversity ASP.NET MVC 5 (.NET Framework 4.8) application to run on .NET 10 and deploys it to Azure App Service. The application currently runs on .NET Framework 4.8 using legacy patterns including MSMQ for messaging, local disk I/O for file uploads, Windows Authentication, hardcoded SQL connection strings, and non-SDK-style project format. The new architecture will:

- Run on .NET 10 (net10.0) with an SDK-style project, ASP.NET Core MVC, and modern middleware pipeline
- Replace MSMQ with Azure Service Bus for reliable cloud messaging
- Use Azure SQL Database with Managed Identity for passwordless database access
- Store uploaded teaching material files in Azure Blob Storage instead of local disk
- Authenticate users via Microsoft Entra ID instead of Windows Authentication
- Externalize configuration to Azure App Configuration
- Deploy to Azure App Service in the SwedenCentral region under resource group `app-mod-cli-full-uni`

The migration follows a phased approach: upgrade the runtime first, then migrate each Azure service integration, remediate security vulnerabilities, provision infrastructure, and finally deploy.

---

## Migration Impact Summary

| Application          | Original Service          | New Azure Service              | Authentication      | Comments                                  |
|----------------------|---------------------------|--------------------------------|---------------------|-------------------------------------------|
| ContosoUniversity    | .NET Framework 4.8        | .NET 10 (net10.0)              | N/A                 | SDK-style project, ASP.NET Core MVC       |
| ContosoUniversity    | MSMQ (System.Messaging)   | Azure Service Bus              | Managed Identity    | Replaces NotificationService.cs           |
| ContosoUniversity    | SQL Server (LocalDB)      | Azure SQL Database             | Managed Identity    | EF Core upgraded, passwordless auth       |
| ContosoUniversity    | Local disk file I/O       | Azure Blob Storage             | Managed Identity    | Teaching material uploads in Courses      |
| ContosoUniversity    | Windows Authentication    | Microsoft Entra ID             | Entra ID            | Replaces IIS Windows Auth in Web.config   |
| ContosoUniversity    | Web.config / appsettings  | Azure App Configuration        | Managed Identity    | Externalizes non-secret settings          |
| ContosoUniversity    | N/A                       | Azure App Service              | Managed Identity    | SwedenCentral, rg: app-mod-cli-full-uni   |

---

## Migration Tasks

### Task 1 — Upgrade .NET to .NET 10
Upgrade the project from .NET Framework 4.8 to .NET 10 (net10.0). Convert to SDK-style project format, migrate ASP.NET MVC 5 to ASP.NET Core MVC, replace Global.asax with Program.cs/Startup middleware, convert route registration, remove incompatible NuGet packages, and resolve all binary/source incompatibilities.

### Task 2 — Migrate MSMQ to Azure Service Bus
Replace the MSMQ-based `NotificationService` with Azure Service Bus. Migrate all `System.Messaging` usage to the Azure Service Bus SDK with Managed Identity authentication.

### Task 3 — Migrate SQL Database to Azure SQL Database
Migrate the SQL Server connection string to Azure SQL Database using Managed Identity (passwordless). Upgrade EF Core to the version compatible with .NET 10.

### Task 4 — Migrate Local File I/O to Azure Blob Storage
Replace the local disk file upload logic in `CoursesController` (teaching material images) with Azure Blob Storage. Migrate `Server.MapPath` and `Directory`/`File` operations to the Azure Storage Blob SDK with Managed Identity.

### Task 5 — Migrate Windows Authentication to Microsoft Entra ID
Replace Windows Authentication configured in Web.config/IISExpress settings with Microsoft Entra ID (formerly Azure AD) for user authentication.

### Task 6 — Configure Console Logging
Set up structured console logging compatible with Azure App Service log aggregation to replace `System.Diagnostics.Debug.WriteLine` patterns.

### Task 7 — Migrate App Settings to Azure App Configuration
Externalize non-secret application settings from Web.config/appsettings to Azure App Configuration, emitting a `.azure/configuration-migration.json` seed file for the deployment agent.

### Task 8 — Security & CVE Remediation
Scan all project dependencies for known CVEs and remediate identified vulnerabilities, including the flagged NuGet package security vulnerability.

### Task 9 — Provision Azure Infrastructure
Generate Bicep IaC files and provision all required Azure resources in the SwedenCentral region under resource group `app-mod-cli-full-uni` (Azure App Service Plan, App Service, Azure SQL Database, Azure Service Bus, Azure Blob Storage, Azure App Configuration).

### Task 10 — Deploy to Azure App Service
Build, containerize if needed, and deploy the upgraded application to Azure App Service in SwedenCentral using the provisioned infrastructure.

---

## Open Questions & Questionnaire

- [x] Q: Should the plan include environment/infrastructure provisioning? → A: Yes — provision new infrastructure in SwedenCentral, resource group `app-mod-cli-full-uni`
- [x] Q: Should the plan include integration testing? → A: Local integration and smoke tests
- [x] Q: Should the plan include security/CVE remediation? → A: Yes — include security/CVE remediation
- [x] Q: Which Azure deployment target? → A: Azure App Service
