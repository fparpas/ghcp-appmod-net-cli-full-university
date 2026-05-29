# Contoso University - GitHub Copilot App Modernization for .NET Demo

This repository contains the Contoso University sample application used to demonstrate **GitHub Copilot app modernization for .NET** in both **Visual Studio Code** and **Visual Studio**.

The demo follows the Microsoft Learn app modernization quickstarts and uses a legacy .NET Framework web application to show how GitHub Copilot can assess modernization readiness, create a migration plan, update code, validate builds, and help move Windows-oriented dependencies to Azure services.

## GitHub Copilot App Mdernization Quickstarts

- [Contoso University migration sample](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/sample)
- [Quickstart for Visual Studio Code](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/quickstart?pivots=vscode)
- [Quickstart for Visual Studio](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/quickstart?pivots=visualstudio)

## Demo Scenario

Contoso University is a fictional university management system built with .NET Framework 4.8. The application includes common line-of-business features such as:

- Student enrollment and profile management
- Course catalog management
- Instructor assignments and office locations
- Department administration
- Teaching material uploads
- Notification messaging

The legacy application uses Windows-based and local development dependencies that are common in older .NET applications:

- **SQL Server LocalDB** for database storage
- **Local file system** access for uploaded teaching materials
- **Microsoft Message Queue (MSMQ)** for notification messaging

During the modernization demo, GitHub Copilot can identify and help migrate these patterns to Azure-native services:

- **Azure SQL Database** instead of SQL Server LocalDB
- **Azure Blob Storage** instead of local file system storage
- **Azure Service Bus** instead of MSMQ
- **Azure Key Vault** for secure secrets management

## Modernization Assessment Report

An aggregated assessment report was generated on **May 29, 2026** and is available at [`modernize/assessment/reports-20260529150535/index.html`](modernize/assessment/reports-20260529150535/index.html).

### Summary

| Attribute | Value |
|---|---|
| Application | ghcp-appmod-net-cli-full-university |
| Component | ContosoUniversity |
| Current Framework | .NET Framework 4.8 |
| Target | .NET 10 on Azure App Service |
| Modernization Strategy | Replatform |
| Estimated Effort | Medium (18 SP) |

### Issues Found

| Severity | Count | Description |
|---|---|---|
| Mandatory | 15 | Blockers that must be resolved before migration |
| Potential | 5 | Recommended to address |
| Optional | 4 | Nice-to-have improvements |

### Recommended Azure Services

| Service | Replaces | Estimated Monthly Cost |
|---|---|---|
| Azure SQL Database (S0) | SQL Server LocalDB | ~$15 |
| Azure Blob Storage (Hot LRS) | Local file system | ~$5 |
| Azure Service Bus (Standard) | MSMQ | ~$10 |
| Azure Key Vault with Managed Identity | Plaintext secrets in config | ~$5 |
| Microsoft Entra ID (P1) | Windows AD / local auth | ~$6 |
| Azure App Service (B1) | Local IIS hosting | ~$32 |
| **Total** | | **~$73/month** |

> **Note:** Cost estimates are directional and based on Azure retail pricing for a dev/test baseline environment. Actual production costs will vary.

