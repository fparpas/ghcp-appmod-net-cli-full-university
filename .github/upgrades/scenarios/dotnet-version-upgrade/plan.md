# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade ContosoUniversity from .NET Framework 4.8 to .NET 10 (net10.0)
**Scope**: 1 project (ContosoUniversity.csproj), 83 files — ASP.NET MVC 5 → ASP.NET Core MVC in-place migration

### Selected Strategy
**All-At-Once** — Single project upgraded in one atomic operation.
**Rationale**: 1 project on net48. Single-project .NET Framework solutions use All-at-Once strategy (no dependency tiers to manage).

---

## Tasks

### 01-sdk-style: Convert ContosoUniversity to SDK-style project format

ContosoUniversity.csproj currently uses the legacy old-style project format with `packages.config`, explicit file includes, and old MSBuild targets imports. This task converts it to SDK-style format while keeping the current target framework (net48), validating that the project builds correctly before any framework changes are introduced.

The conversion covers: removing explicit file globs and redundant assembly references, migrating `packages.config` entries to `PackageReference` in the project file, removing legacy `<Import>` targets (Microsoft.CSharp.targets, Microsoft.WebApplication.targets), and stripping old-style project attributes. The result is a clean SDK-style `.csproj` that retains net48 as its TFM.

Note: After SDK conversion, the project should build successfully on net48 before proceeding to the TFM upgrade task.

**Done when**: ContosoUniversity.csproj is in SDK-style format, all NuGet packages are expressed as `<PackageReference>` entries, `packages.config` is removed, the project builds successfully targeting net48 with 0 errors.

---

### 02-upgrade: Upgrade ContosoUniversity to net10.0 and migrate to ASP.NET Core MVC

This is the core migration task. It covers all changes required to move ContosoUniversity from net48 + ASP.NET MVC 5 to net10.0 + ASP.NET Core MVC using an in-place rewrite approach. The project has 7 controllers and is a demo-scale application.

**Framework migration scope** (from assessment — 96 mandatory issues):
- Change `<TargetFramework>` from `net48` to `net10.0`
- Replace `Global.asax` / `Application_Start` lifecycle with `Program.cs` and the ASP.NET Core middleware pipeline
- Convert `App_Start/RouteConfig.cs` route registrations to ASP.NET Core `MapControllerRoute` / `MapDefaultControllerRoute`
- Remove `System.Web.Optimization` bundle/minification (BundleConfig.cs, `@Scripts.Render`, `@Styles.Render`) — replace with direct `<script>` and `<link>` tags in layout and views
- Migrate `System.Web.Mvc` controller base classes, attributes, and action results to `Microsoft.AspNetCore.Mvc` equivalents across all 7 controllers (BaseController, CoursesController, DepartmentsController, HomeController, InstructorsController, NotificationsController, StudentsController)
- Migrate Razor views: update `@using`, `@Html.*`, `@Url.*`, `@Ajax.*` helpers and layout directives to ASP.NET Core equivalents; replace `_ViewStart.cshtml` / `_Layout.cshtml` MVC 5 patterns
- Convert `web.config` configuration (connection strings, app settings) to `appsettings.json` and ASP.NET Core `IConfiguration`
- Remove assembly binding redirects from `web.config` (Binding.0006: 6 instances, Binding.0007: 6 instances)
- Convert MSMQ-based notification system (`Services/` using `System.Messaging`) to an equivalent .NET Core compatible approach (Azure Service Bus client or in-memory channel, or stub with comment explaining replacement options)

**Package changes** (from assessment):
- Remove incompatible packages: `Microsoft.AspNet.Web.Optimization`, `WebGrease`, `Antlr` (3.x)
- Remove framework-included packages: `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`, `Microsoft.Web.Infrastructure`, `NETStandard.Library`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `System.Buffers`, `System.Memory`, `System.Numerics.Vectors`, `System.Threading.Tasks.Extensions`, `System.ComponentModel.Annotations`
- Add: `Microsoft.AspNetCore.Mvc` (via `Microsoft.AspNetCore.App` framework reference)
- Upgrade EF Core packages from 3.1.32 → 10.0.x: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, and all related EF Core packages
- Fix security vulnerability: upgrade `Microsoft.Data.SqlClient` from 2.1.4 → 7.0.1
- Remove deprecated: `Microsoft.Identity.Client` 4.21.1 (or upgrade if used)
- Upgrade remaining packages to net10.0-compatible versions: `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Bcl.HashCode`, `Microsoft.Extensions.*` packages (3.1.32 → 10.0.x), `System.Collections.Immutable`, `System.Diagnostics.DiagnosticSource`, `System.Runtime.CompilerServices.Unsafe`, `Newtonsoft.Json`

**API incompatibilities** (Api.0001: 63 binary-incompatible, Api.0002: 29 source-incompatible — primarily in System.Web and System.Messaging namespaces):
- Replace all `System.Web.*` API calls with ASP.NET Core equivalents
- Replace or stub `System.Messaging.*` (MSMQ) API calls

**Done when**: ContosoUniversity.csproj targets net10.0, the solution builds with 0 errors and 0 warnings in modified files, all existing tests pass, and no references to `System.Web` or `System.Messaging` remain in compilable code.

---

### 03-validation: Final validation and cleanup

Verify the complete upgrade is coherent: full solution build, test suite run, cleanup of any temporary stubs or comments introduced during migration, and documentation of any deferred items (e.g., MSMQ production replacement choice) as post-upgrade recommendations.

**Done when**: Solution builds 0 errors, all tests pass, no STUB comments remain without resolution, post-upgrade recommendations documented.
