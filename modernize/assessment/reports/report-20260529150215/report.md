# ContosoUniversity

## Summary

| Metric | Value |
|--------|-------|
| Total Issues | 6 |
| Mandatory Blockers | 2 |
| Potential Issues | 2 |

## Component Information

| Property | Value |
|----------|-------|
| Language | C# |
| Frameworks | .NETFramework,Version=v4.8 |
| Build tools | MSBuild |

## Cloud Readiness Issues

| Issue Name | Criticality | Story Points | Occurrences |
|------------|-------------|--------------|-------------|
| MSMQ usage is detected | Mandatory | 3 | [12](#MSMQ_usage_is_detected) |
| Windows authentication detected | Mandatory | 3 | [1](#Windows_authentication_detected) |
| Local or network IO operations detected | Potential | 3 | [8](#Local_or_network_IO_operations_detected) |
| SQL database connection detected | Potential | 3 | [1](#SQL_database_connection_detected) |
| Connection strings without configuration builders detected | Optional | 3 | [2](#Connection_strings_without_configuration_builders_detected) |
| Static content detected | Optional | 3 | [1](#Static_content_detected) |

### Issue Details

<details id="MSMQ_usage_is_detected">
<summary><b>MSMQ usage is detected</b> — affected files</summary>

- `Services\NotificationService.cs (line 21, col 25)`
- `Services\NotificationService.cs (line 26, col 29)`
- `Services\NotificationService.cs (line 30, col 35)`
- `Services\NotificationService.cs (line 22, col 50)`
- `Services\NotificationService.cs (line 19, col 17)`
- `Services\NotificationService.cs (line 11, col 25)`
- `Services\NotificationService.cs (line 77, col 19)`
- `Services\NotificationService.cs (line 77, col 79)`
- `Services\NotificationService.cs (line 73, col 16)`
- `Services\NotificationService.cs (line 54, col 16)`
- `Services\NotificationService.cs (line 54, col 34)`
- `Services\NotificationService.cs (line 57, col 31)`

</details>

<details id="Windows_authentication_detected">
<summary><b>Windows authentication detected</b> — affected files</summary>

- `Web.config`

</details>

<details id="Local_or_network_IO_operations_detected">
<summary><b>Local or network IO operations detected</b> — affected files</summary>

- `Controllers\CoursesController.cs (line 161, col 28)`
- `Controllers\CoursesController.cs (line 78, col 28)`
- `Controllers\CoursesController.cs (line 159, col 29)`
- `Controllers\CoursesController.cs (line 76, col 29)`
- `Controllers\CoursesController.cs (line 229, col 30)`
- `Controllers\CoursesController.cs (line 172, col 42)`
- `Controllers\CoursesController.cs (line 233, col 34)`
- `Controllers\CoursesController.cs (line 174, col 42)`

</details>

<details id="SQL_database_connection_detected">
<summary><b>SQL database connection detected</b> — affected files</summary>

- `Web.config`

</details>

<details id="Connection_strings_without_configuration_builders_detected">
<summary><b>Connection strings without configuration builders detected</b> — affected files</summary>

- `Web.config`

</details>

<details id="Static_content_detected">
<summary><b>Static content detected</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

## DotNET Upgrade Issues [View Details](scenarios/dotnet-version-upgrade/assessment.md)

| Issue Category | Criticality | Story Points |
|----------------|-------------|--------------|
| Binary incompatible for selected .NET version | Mandatory | 1 |
| NuGet package functionality is included with framework reference | Mandatory | 1 |
| System.Web.Optimization bundling and minification is not supported in .NET Core and should be replaced with actual html tags pointing to content files | Mandatory | 1 |
| Manual redirect conflicts with auto-generated version | Mandatory | 1 |
| Routes registration via RouteCollection is not supported in .NET Core and needs to be converted to the route mappings on the application object | Mandatory | 1 |
| NuGet package is incompatible | Mandatory | 1 |
| Project file needs to be converted to SDK-style | Mandatory | 1 |
| Project's target framework(s) needs to be changed | Mandatory | 1 |
| Convert application initialization code from Global.asax.cs to .NET Core and clean up Global.asax.cs | Mandatory | 1 |
| Convert System.Messaging to MSMQ in .NET Core | Mandatory | 1 |
| MSMQ & Message Queuing | Mandatory | 2 |
| Legacy Configuration System | Mandatory | 2 |
| ASP.NET Framework (System.Web) | Mandatory | 4 |
| Source incompatible for selected .NET version | Potential | 1 |
| NuGet package upgrade is recommended | Potential | 1 |
| Binding redirect forces version downgrade | Potential | 1 |
| NuGet package is deprecated | Optional | 1 |
| NuGet package contains security vulnerability | Optional | 1 |

### Issue Details

<details>
<summary><b>Binary incompatible for selected .NET version</b> — affected files</summary>

- `Services\NotificationService.cs (line 116, col 12)`
- `Services\NotificationService.cs (line 71, col 12)`
- `Services\NotificationService.cs (line 74, col 16)`
- `Services\NotificationService.cs (line 73, col 16)`
- `Services\NotificationService.cs (line 60, col 16)`
- `Services\NotificationService.cs (line 54, col 16)`
- `Services\NotificationService.cs (line 30, col 12)`
- `Services\NotificationService.cs (line 26, col 16)`
- `Services\NotificationService.cs (line 22, col 16)`
- `Services\NotificationService.cs (line 21, col 16)`
- `Services\NotificationService.cs (line 19, col 12)`
- `Services\NotificationService.cs (line 11, col 38)`
- `Global.asax.cs (line 17, col 12)`
- `App_Start\RouteConfig.cs (line 7, col 8)`

</details>

<details>
<summary><b>NuGet package functionality is included with framework reference</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details>
<summary><b>System.Web.Optimization bundling and minification is not supported in .NET Core and should be replaced with actual html tags pointing to content files</b> — affected files</summary>

- `Views\Courses\Create.cshtml`
- `Views\Courses\Edit.cshtml`
- `Views\Departments\Create.cshtml`
- `Views\Departments\Edit.cshtml`
- `Views\Instructors\Create.cshtml`
- `Views\Instructors\Edit.cshtml`
- `Views\Shared\_Layout.cshtml`
- `Views\Students\Create.cshtml`
- `Views\Students\Edit.cshtml`

</details>

<details>
<summary><b>Manual redirect conflicts with auto-generated version</b> — affected files</summary>

- `Web.config`

</details>

<details>
<summary><b>Routes registration via RouteCollection is not supported in .NET Core and needs to be converted to the route mappings on the application object</b> — affected files</summary>

- `Global.asax.cs`
- `App_Start\RouteConfig.cs`

</details>

<details>
<summary><b>NuGet package is incompatible</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details>
<summary><b>Project file needs to be converted to SDK-style</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details>
<summary><b>Project's target framework(s) needs to be changed</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details>
<summary><b>Convert application initialization code from Global.asax.cs to .NET Core and clean up Global.asax.cs</b> — affected files</summary>

- `Global.asax.cs`

</details>

<details>
<summary><b>Convert System.Messaging to MSMQ in .NET Core</b> — affected files</summary>

- `Services\NotificationService.cs`

</details>

<details>
<summary><b>Source incompatible for selected .NET version</b> — affected files</summary>

- `Services\NotificationService.cs (line 73, col 16)`
- `Services\NotificationService.cs (line 16, col 12)`
- `Data\SchoolContextFactory.cs (line 9, col 12)`
- `Controllers\CoursesController.cs (line 127, col 8)`
- `Controllers\CoursesController.cs (line 179, col 24)`
- `Controllers\CoursesController.cs (line 148, col 20)`
- `Controllers\CoursesController.cs (line 138, col 20)`
- `Controllers\CoursesController.cs (line 134, col 16)`
- `Controllers\CoursesController.cs (line 44, col 8)`
- `Controllers\CoursesController.cs (line 86, col 24)`
- `Controllers\CoursesController.cs (line 65, col 20)`
- `Controllers\CoursesController.cs (line 55, col 20)`
- `Controllers\CoursesController.cs (line 51, col 16)`
- `Global.asax.cs (line 26, col 12)`
- `Global.asax.cs (line 11, col 34)`

</details>

<details>
<summary><b>NuGet package upgrade is recommended</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details>
<summary><b>Binding redirect forces version downgrade</b> — affected files</summary>

- `Web.config`

</details>

<details>
<summary><b>NuGet package is deprecated</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details>
<summary><b>NuGet package contains security vulnerability</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

---

## Codebase Insights

> **Note:** These documents are generated by AI and may contain inaccuracies or incomplete information. Please review carefully.

1. **[Architecture Diagram](facts/architecture-diagram.md)** — Understand the big picture: system layers and component relationships
2. **[Dependency Map](facts/dependency-map.md)** — Know what the project depends on and where the risks are
3. **[API & Service Contracts](facts/api-service-contracts.md)** — See how services communicate and what contracts they expose
4. **[Data Architecture](facts/data-architecture.md)** — Explore data models, storage, and data flow patterns
5. **[Configuration Inventory](facts/configuration-inventory.md)** — Review how the application is configured across environments
6. **[Business Workflows](facts/business-workflows.md)** — Trace end-to-end business processes and domain logic

[Share feedback](https://aka.ms/ghcp-appmod/feedback)
