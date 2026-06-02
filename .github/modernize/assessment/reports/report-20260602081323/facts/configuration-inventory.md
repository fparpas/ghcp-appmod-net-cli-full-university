# Configuration & Externalized Settings Inventory

This ASP.NET MVC 5 application uses a single `Web.config` as its sole configuration source, with two MSBuild build configurations (Debug/Release) and no runtime profile system — all environment-specific overrides must be applied manually or via Web.config transforms.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|--------|------|---------------|-------|
| `Web.config` | XML App Config | `/Web.config` | Primary configuration: connection strings, app settings, httpRuntime, assembly binding redirects |
| `Views\Web.config` | XML App Config | `/Views/Web.config` | Razor view engine configuration; defines MVC view namespaces and blocks direct view access |
| `packages.config` | NuGet Package Manifest | `/packages.config` | NuGet package references for .NET Framework 4.8 (classic-style, not SDK-style) |
| `ContosoUniversity.csproj` | MSBuild Project File | `/ContosoUniversity.csproj` | Build configurations (Debug/Release), IIS Express settings, reference hints |
| `System.Configuration.ConfigurationManager` | Runtime API | (in-process) | Used by `SchoolContextFactory`, `NotificationService`, and `Global.asax.cs` to read connection strings and app settings at runtime |

> Note: No `appsettings.json`, `launchSettings.json`, environment-specific `Web.{env}.config` transforms, Docker Compose files, Kubernetes manifests, or external config server references were found. Web.Debug.config and Web.Release.config referenced in the `.csproj` were not present in the repository.

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---------|-----------|---------|--------------------------|
| `Debug` | Default (when `$(Configuration)` is unset) | Development builds: full debug symbols, no optimization, `DEBUG;TRACE` constants | `DebugSymbols=true`, `DebugType=full`, `Optimize=false`, output to `bin\` |
| `Release` | Manual (`-p:Configuration=Release` or VS publish) | Production builds: PDB-only symbols, optimizations enabled, `TRACE` constant only | `DebugSymbols=true`, `DebugType=pdbonly`, `Optimize=true`, output to `bin\` |

**Post-build target (`CopySQLClientNativeBinaries`)**: Always runs after build; copies `Microsoft.Data.SqlClient.SNI.dll` (native) for both x64 and x86 to the output directory to support encrypted SQL Server connections.

**`MvcBuildViews`** target: Defined but disabled (`MvcBuildViews=false`); when enabled, compiles Razor views during build using `AspNetCompiler`.

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---------|------------------|--------------|---------------|
| *(none — single profile)* | N/A | `Web.config` (always loaded) | No runtime profile switching; all settings are static in `Web.config` |

> Note: This application does not use ASP.NET Core's environment-based profile system (`ASPNETCORE_ENVIRONMENT`) or `appsettings.{Environment}.json`. The classic ASP.NET pipeline reads only `Web.config`. Environment differentiation would require Web.config transform files (`Web.Debug.config`, `Web.Release.config`), which are referenced in the `.csproj` but absent from the repository.

## Properties Inventory

### Connection Strings (`<connectionStrings>`)

| Property Key | Default Value | Profiles | Source |
|-------------|---------------|---------|--------|
| `DefaultConnection` | `Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=ContosoUniversityNoAuthEFCore;Integrated Security=True;MultipleActiveResultSets=True` | All | `Web.config` — read via `ConfigurationManager.ConnectionStrings["DefaultConnection"]` in `SchoolContextFactory.Create()` and `Global.asax.cs:InitializeDatabase()` |

### Application Settings (`<appSettings>`)

| Property Key | Default Value | Profiles | Source |
|-------------|---------------|---------|--------|
| `webpages:Version` | `3.0.0.0` | All | `Web.config` — WebPages framework version |
| `webpages:Enabled` | `false` | All | `Web.config` — disables WebPages routing |
| `ClientValidationEnabled` | `true` | All | `Web.config` — enables jQuery client-side validation |
| `UnobtrusiveJavaScriptEnabled` | `true` | All | `Web.config` — enables unobtrusive JS for validation |
| `NotificationQueuePath` | `.\Private$\ContosoUniversityNotifications` | All | `Web.config` — MSMQ private queue path; read in `NotificationService` constructor via `ConfigurationManager.AppSettings["NotificationQueuePath"]` |

### HTTP Runtime Settings (`<system.web>/<httpRuntime>`)

| Property Key | Default Value | Profiles | Source |
|-------------|---------------|---------|--------|
| `compilation/@debug` | `true` | All | `Web.config` — should be `false` in production |
| `httpRuntime/@targetFramework` | `4.8` | All | `Web.config` |
| `httpRuntime/@maxRequestLength` | `10240` (KB = 10 MB) | All | `Web.config` — controls max upload size for file uploads |
| `httpRuntime/@executionTimeout` | `3600` (seconds = 1 hour) | All | `Web.config` — extended for large file upload operations |

### IIS / Web Server Settings (`<system.webServer>`)

| Property Key | Default Value | Profiles | Source |
|-------------|---------------|---------|--------|
| `requestFiltering/requestLimits/@maxAllowedContentLength` | `10485760` (bytes = 10 MB) | All | `Web.config` — IIS-level upload size limit (must align with `maxRequestLength`) |
| `validation/@validateIntegratedModeConfiguration` | `false` | All | `Web.config` — suppresses IIS integrated mode config warnings |

### IIS Express Development Settings (`.csproj`)

| Property Key | Default Value | Source |
|-------------|---------------|--------|
| `DevelopmentServerPort` | `58801` | `ContosoUniversity.csproj` |
| `IISUrl` | `https://localhost:44300/` | `ContosoUniversity.csproj` |
| `IISExpressWindowsAuthentication` | `enabled` | `ContosoUniversity.csproj` |
| `IISExpressAnonymousAuthentication` | `disabled` | `ContosoUniversity.csproj` |
| `UseIIS` | `True` | `ContosoUniversity.csproj` |
| `AutoAssignPort` | `True` | `ContosoUniversity.csproj` |

## Startup Parameters & Resource Requirements

| Service | Runtime Options | Memory | Instance Count |
|---------|----------------|--------|---------------|
| ContosoUniversity (IIS Express, dev) | .NET Framework 4.8 CLR; no explicit JVM-equivalent heap settings; IIS Express uses system defaults | Not specified; Windows manages CLR memory automatically | 1 (single IIS worker process) |
| ContosoUniversity (IIS, production) | .NET Framework 4.8; application pool settings managed via IIS Manager or `applicationHost.config` (not in repo) | Not specified in repository | Not specified; IIS app pool recycling applies |

> Note: No Docker Compose, Kubernetes, or container definitions exist. No JVM/CLR startup heap overrides, `-D` system properties, or `ASPNETCORE_ENVIRONMENT` variables are configured. IIS application pool configuration is external to this repository.

## Startup Dependency Chain

1. **IIS / IIS Express** starts and loads the ASP.NET application pool
2. **`Application_Start()` in `Global.asax.cs`** executes sequentially:
   - `AreaRegistration.RegisterAllAreas()` — registers MVC areas (none currently defined)
   - `FilterConfig.RegisterGlobalFilters()` — registers `HandleErrorAttribute` global filter
   - `RouteConfig.RegisterRoutes()` — registers the default `{controller}/{action}/{id}` route
   - `BundleConfig.RegisterBundles()` — registers CSS and JS bundles
   - `InitializeDatabase()` — reads `DefaultConnection` from `Web.config`, creates `DbContextOptionsBuilder`, instantiates `SchoolContext`, calls `DbInitializer.Initialize(context)` (seeds database if empty)
3. **SQL Server / LocalDB** must be accessible before `InitializeDatabase()` completes; no retry or circuit-breaker logic is present — a connection failure during startup will throw an unhandled exception
4. **MSMQ** (`.\Private$\ContosoUniversityNotifications`) is initialized lazily on first use of `NotificationService` (not at startup); if the queue does not exist, `NotificationService` creates it automatically

**Wait mechanisms**: None. No health checks, readiness probes, `dockerize` wait-for-TCP, or retry policies are implemented. SQL Server connectivity failure during `Application_Start` will prevent the application from starting.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage |
|-----------------|------|---------|
| `DefaultConnection` connection string | SQL Server connection string (Windows Integrated Security) | `Web.config` — uses Windows Integrated Security (`Integrated Security=True`); **no password in connection string** |
| MSMQ queue path | Infrastructure path | `Web.config` `appSettings` — `NotificationQueuePath`; not a secret but infrastructure-specific |

### Secrets Provisioning Workflow

**Current state**: This application relies exclusively on **Windows Integrated Security** for database access, meaning the IIS application pool identity (or the developer's Windows account under IIS Express) is used to authenticate to SQL Server. There are no passwords, API keys, or secrets stored anywhere in the configuration files.

- **Secret source**: Windows Authentication / Integrated Security — no explicit credentials required
- **Identity/access model**: The Windows account running the IIS application pool must have `db_owner` or appropriate SQL Server permissions on the `ContosoUniversityNoAuthEFCore` database
- **Provisioning sequence**: No automated secrets provisioning; DBA grants SQL Server permissions to the IIS service account manually
- **Services needing secrets**: Only `SchoolContext` (via `DefaultConnection`) — no external APIs, no message broker credentials, no encryption keys

> Note: No HashiCorp Vault, Azure Key Vault, AWS Secrets Manager, Jasypt encryption, DPAPI, or sealed secrets are used. Moving to a cloud deployment would require introducing explicit credentials and a secrets management strategy.

## Feature Flags

| Flag Name | Default | Controlled By |
|-----------|---------|--------------|
| `webpages:Enabled` | `false` | `Web.config` `appSettings` — disables ASP.NET WebPages engine |
| `ClientValidationEnabled` | `true` | `Web.config` `appSettings` — enables jQuery client-side validation globally |
| `UnobtrusiveJavaScriptEnabled` | `true` | `Web.config` `appSettings` — enables unobtrusive JavaScript for forms |
| `MvcBuildViews` | `false` | `ContosoUniversity.csproj` MSBuild property — controls compile-time Razor view compilation |
| `compilation/@debug` | `true` | `Web.config` — enables debug mode (should be `false` in production) |
| `HandleErrorAttribute` (global filter) | Enabled | `FilterConfig.cs` — catches unhandled exceptions and renders `~/Views/Shared/Error.cshtml` |
| Authorization filter | Disabled (commented out) | `FilterConfig.cs` — `AuthorizeAttribute` global filter is commented out; no global authentication enforcement |

> Note: No dedicated feature-flag framework (LaunchDarkly, Unleash, .NET `Microsoft.FeatureManagement`) is used. All toggles are static configuration values.

## Framework & Runtime Versions

| Component | Version | Source |
|-----------|---------|--------|
| Target Framework | .NET Framework 4.8 | `ContosoUniversity.csproj` `TargetFrameworkVersion` |
| ASP.NET MVC | 5.2.9 | `packages.config`, `Web.config` binding redirects |
| ASP.NET Razor | 3.2.9 | `packages.config` |
| ASP.NET WebPages | 3.2.9 | `packages.config` |
| Entity Framework Core | 3.1.32 | `packages.config` |
| EF Core SQL Server Provider | 3.1.32 | `packages.config` |
| Microsoft.Data.SqlClient | 2.1.4 | `packages.config` |
| Microsoft.Extensions.DependencyInjection | 3.1.32 | `packages.config` |
| Microsoft.Extensions.Configuration | 3.1.32 | `packages.config` |
| Microsoft.Extensions.Logging | 3.1.32 | `packages.config` |
| Microsoft.Extensions.Caching.Memory | 3.1.32 | `packages.config` |
| Microsoft.Identity.Client (MSAL) | 4.21.1 | `packages.config` |
| Newtonsoft.Json | 13.0.3 | `packages.config` |
| Bootstrap | 5.3.3 | `packages.config` (CSS/JS) |
| jQuery | 3.7.1 | `packages.config` |
| jQuery Validation | 1.21.0 | `packages.config` |
| jQuery Unobtrusive Validation | 4.0.0 | `packages.config` |
| Modernizr | 2.6.2 | `packages.config` |
| WebGrease | 1.5.2 | `packages.config` (asset bundling) |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 2.0.1 | `packages.config` (Roslyn compiler) |
| NETStandard.Library | 2.0.3 | `packages.config` (compatibility shim) |
| Build Tool | MSBuild (ToolsVersion 15.0) | `ContosoUniversity.csproj` |
| Package Manager | NuGet (packages.config format) | `packages.config` |
| IIS Express (dev server) | IIS Express (version from VS install) | `ContosoUniversity.csproj` |
| Assembly Version | 1.0.0.0 | `Properties\AssemblyInfo.cs` |
