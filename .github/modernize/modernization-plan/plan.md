# Modernization Plan: modernization-plan

**Project**: ContosoUniversity

---

## Technical Framework

- **Language**: C# / .NET Framework 4.8
- **Framework**: ASP.NET MVC 5 (System.Web)
- **Build Tool**: MSBuild (legacy non-SDK-style project)
- **Database**: SQL Server (LocalDB / SQL Server via Entity Framework Core 3.1)
- **Messaging**: MSMQ (System.Messaging) — NotificationService
- **File Storage**: Local file system (~/Uploads/TeachingMaterials/)
- **Authentication**: Windows Authentication (IIS Express / Web.config)
- **Key Dependencies**: Microsoft.AspNet.Mvc 5.2.9, Microsoft.EntityFrameworkCore 3.1.32, System.Messaging, Newtonsoft.Json 13.0.3, Microsoft.AspNet.Web.Optimization 1.1.3

---

## Overview

This migration upgrades ContosoUniversity from .NET Framework 4.8 (ASP.NET MVC 5) to .NET 10, and modernizes it for deployment on Azure App Service. The application currently uses legacy .NET Framework APIs (System.Web, MSMQ, local file I/O, Windows Authentication, and hardcoded connection strings in Web.config). The new architecture will:

- Run on .NET 10 with a modern SDK-style project structure (ASP.NET Core MVC), eliminating all System.Web and legacy ASP.NET Framework dependencies
- Replace MSMQ-based notifications with Azure Service Bus using Managed Identity authentication, enabling cloud-native asynchronous messaging
- Replace the local SQL Server connection string with Azure SQL Database using Managed Identity (passwordless), removing hardcoded credentials from configuration
- Replace local file system storage for teaching material uploads with Azure Storage Blob, enabling scalable and cloud-native file storage
- Replace Windows Authentication with Microsoft Entra ID for modern, cloud-compatible identity management
- Externalize non-secret application settings from Web.config to Azure App Configuration for centralized configuration management
- Be deployed to Azure App Service with Bicep-provisioned infrastructure

The migration follows a phased approach: first upgrading the runtime framework, then migrating individual Azure service integrations, remediating security vulnerabilities, provisioning infrastructure with Bicep, and finally deploying to Azure App Service.

---

## Migration Impact Summary

| Application          | Original Service            | New Azure Service              | Authentication     | Comments                                      |
|----------------------|-----------------------------|--------------------------------|--------------------|-----------------------------------------------|
| ContosoUniversity    | .NET Framework 4.8 / MVC 5  | .NET 10 / ASP.NET Core MVC     | N/A                | SDK-style project, Global.asax → Program.cs   |
| ContosoUniversity    | MSMQ (System.Messaging)     | Azure Service Bus              | Managed Identity   | NotificationService.cs migration              |
| ContosoUniversity    | SQL Server (LocalDB)        | Azure SQL Database             | Managed Identity   | EF Core upgraded, passwordless connection     |
| ContosoUniversity    | Local file system           | Azure Storage Blob             | Managed Identity   | CoursesController teaching material uploads  |
| ContosoUniversity    | Windows Authentication      | Microsoft Entra ID             | Entra ID (OIDC)    | Web.config / IIS Express auth replacement    |
| ContosoUniversity    | Web.config app settings     | Azure App Configuration        | Managed Identity   | Non-secret settings externalized             |
| ContosoUniversity    | N/A                         | Azure App Service              | N/A                | Bicep IaC, azcli deployment                  |

---

## Open Questions & Questionnaire

- [x] Q: Should the plan include environment/infrastructure provisioning? → A: Yes — generate Bicep IaC files for Azure resources
- [x] Q: Should the plan include integration testing? → A: No — skipped (no environment specified by user)
- [x] Q: Should the plan include a security/CVE remediation task? → A: Yes — include security/CVE remediation
- [x] Q: Which Azure deployment target should the plan use? → A: Azure App Service
