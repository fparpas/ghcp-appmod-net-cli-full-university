# Configuration & Externalized Settings Inventory

This ASP.NET MVC 5 application on .NET Framework 4.8 relies on a single `Web.config` file as its sole configuration source, with no environment-specific transform files, external config servers, or secret stores — all settings including the database connection string are statically embedded.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|--------|------|---------------|-------|
| Web.config | XML App Config | `/Web.config` | Primary configuration file; contains connection strings, appSettings, HTTP runtime settings, and assembly binding redirects |
| Views/Web.config | XML App Config | `/Views/Web.config` | Razor view engine configuration; defines MVC view page base types and namespace imports |
| packages.config | NuGet manifest | `/packages.config` | Declares all NuGet package dependencies with versions; consumed by MSBuild/NuGet restore |
| ContosoUniversity.csproj | MSBuild project file | `/ContosoUniversity.csproj` | Defines build configurations (Debug/Release), IIS Express settings, assembly references |

> Note: No `appsettings.json`, `launchSettings.json`, environment-specific transform files (`Web.Debug.config`, `Web.Release.config`), external config servers, secret stores, or Docker/Kubernetes configuration files were found.

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---------|-----------|---------|--------------------------|
| Debug | Default (no `-p:Configuration` flag needed) or `msbuild /p:Configuration=Debug` | Development build with full debug symbols, no optimization; includes `DEBUG;TRACE` constants | DebugSymbols=true, DebugType=full, Optimize=false, OutputPath=`bin\` |
| Release | `msbuild /p:Configuration=Release` | Production build with PDB-only symbols and optimization enabled; defines `TRACE` constant only | DebugSymbols=true, DebugType=pdbonly, Optimize=true, OutputPath=`bin\` |

**Post-build task (both configurations):** `CopySQLClientNativeBinaries` — copies `Microsoft.Data.SqlClient.SNI.dll` (x64 and x86) from NuGet packages to the output `bin\` directory for SQL Server native connectivity.

**MVC View compilation:** `MvcBuildViews` is set to `false` — Razor views are not pre-compiled at build time; they compile on first request.

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---------|------------------|--------------|---------------|
| (Single profile only) | N/A — no runtime profile system configured | `Web.config` | No environment-based overrides; all settings are static |

> Note: The application does not use `ASPNETCORE_ENVIRONMENT`, Web.config XDT transforms, or any profile/environment activation mechanism. A single `Web.config` serves all runtime environments.

## Properties Inventory

### ContosoUniversity — Application Settings (`<appSettings>`)

| Property Key | Default Value | Profiles | Source |
|-------------|---------------|---------|--------|
| `webpages:Version` | `3.0.0.0` | All | `Web.config` |
| `webpages:Enabled` | `false` | All | `Web.config` |
| `ClientValidationEnabled` | `true` | All | `Web.config` |
| `UnobtrusiveJavaScriptEnabled` | `true` | All | `Web.config` |
| `NotificationQueuePath` | `.\Private$\ContosoUniversityNotifications` | All | `Web.config` |

### ContosoUniversity — Connection Strings (`<connectionStrings>`)

| Name | Default Value | Profiles | Source |
|------|---------------|---------|--------|
| `DefaultConnection` | `Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=ContosoUniversityNoAuthEFCore;Integrated Security=True;MultipleActiveResultSets=True` | All | `Web.config` |

### ContosoUniversity — HTTP Runtime (`<system.web>`)

| Property | Value | Notes |
|---------|-------|-------|
| `compilation[@debug]` | `true` | Debug compilation enabled (should be `false` in production) |
| `compilation[@targetFramework]` | `4.8` | Target .NET Framework version |
| `httpRuntime[@targetFramework]` | `4.8` | HTTP runtime target framework |
| `httpRuntime[@maxRequestLength]` | `10240` (KB = 10 MB) | Maximum HTTP request size |
| `httpRuntime[@executionTimeout]` | `3600` seconds (1 hour) | Request execution timeout |
| `requestLimits[@maxAllowedContentLength]` | `10485760` bytes (10 MB) | IIS request size limit (must align with maxRequestLength) |
| `validateIntegratedModeConfiguration` | `false` | Suppresses IIS integrated mode validation warnings |

### Views/Web.config — Razor View Engine Settings

| Property | Value | Source |
|---------|-------|--------|
| Razor host factory | `System.Web.Mvc.MvcWebRazorHostFactory` (MVC 5.2.9.0) | `Views/Web.config` |
| View page base type | `System.Web.Mvc.WebViewPage` | `Views/Web.config` |
| webpages:Version | `3.0.0.0` | `Views/Web.config` |
| webpages:Enabled | `false` | `Views/Web.config` |
| validateRequest | `false` | `Views/Web.config` — disables request validation for Razor views |
| Default namespaces imported | `System.Web.Mvc`, `System.Web.Mvc.Ajax`, `System.Web.Mvc.Html`, `System.Web.Optimization`, `ContosoUniversity` | `Views/Web.config` |

### IIS Express Development Server (from `.csproj`)

| Property | Value | Notes |
|---------|-------|-------|
| Development server port | `58801` | HTTP port for Visual Studio development server |
| IIS URL | `https://localhost:44300/` | HTTPS URL used in Visual Studio IIS Express |
| IISExpressWindowsAuthentication | `enabled` | Windows auth enabled in IIS Express |
| IISExpressAnonymousAuthentication | `disabled` | Anonymous auth disabled in IIS Express |
| UseIIS | `True` | Uses IIS Express (not Cassini/built-in server) |
| AutoAssignPort | `True` | Automatically assigns port if default is taken |

## Startup Parameters & Resource Requirements

| Service | Runtime Options | Memory | Instance Count |
|---------|----------------|--------|---------------|
| ContosoUniversity (IIS Express) | Hosted by IIS Express or IIS; no JVM or explicit heap settings (CLR managed heap) | Not explicitly configured; relies on IIS/OS defaults | Single instance; no scaling configuration |

> Note: This is a classic ASP.NET MVC application running on the CLR (not .NET Core). There are no JVM parameters, `-Xms`/`-Xmx` settings, Docker containers, Kubernetes manifests, or explicit memory/CPU limits defined.

**Runtime dependencies resolved at startup (`Global.asax.cs > Application_Start`):**
1. Area registration
2. Global filter registration (`HandleErrorAttribute`)
3. Route registration (default MVC route)
4. Bundle registration (jQuery, Bootstrap, Modernizr, CSS)
5. Database initialization via EF Core (`DbInitializer.Initialize`) using `DefaultConnection` from `Web.config`

## Startup Dependency Chain

| Order | Component | Waits For | Mechanism |
|-------|-----------|-----------|-----------|
| 1 | IIS / IIS Express | OS and .NET Framework 4.8 runtime | Process startup |
| 2 | ASP.NET MVC application | IIS pipeline initialization | `Application_Start` in `Global.asax` |
| 3 | EF Core / SQL Server | `DefaultConnection` string availability | `DbInitializer.Initialize()` called synchronously on startup; will throw if SQL Server (LocalDB) is unavailable |
| 4 | MSMQ (`NotificationService`) | MSMQ service on local machine | `MessageQueue.Exists()` / `MessageQueue.Create()` called on first `NotificationService` instantiation; queue path: `.\Private$\ContosoUniversityNotifications` |

> Note: There are no Docker Compose `depends_on`, Kubernetes readiness probes, Spring Cloud Config retry, or `dockerize` wait mechanisms. SQL Server LocalDB must be available before first request that triggers database access.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage |
|-----------------|------|---------|
| `DefaultConnection` connection string | SQL Server connection string | Hardcoded in `Web.config` (plain text); uses Windows Integrated Security — **no password in connection string** |

> **No explicit secrets** (passwords, API keys, tokens) are present in the configuration files. The database connection uses `Integrated Security=True`, relying on the Windows identity of the application pool.

### Secrets Provisioning Workflow

**Current approach:** No secrets management system is in place.

- **Secret source:** `Web.config` (static file, plain text)
- **Identity/access model:** Windows Integrated Security (`Integrated Security=True`) for SQL Server; the application pool identity (or developer's Windows account under IIS Express) is used for database access — no database password is stored
- **Provisioning sequence:** Configuration is static; no deployment pipeline secrets injection, no Key Vault binding, no environment variable substitution
- **MSMQ access:** Queue permissions are set programmatically (`"Everyone"` with `FullControl`) on first run — this is a security concern in production environments

> **Risk:** `compilation debug="true"` is hardcoded in `Web.config`, which should be `false` for production. There is no mechanism (transform, environment variable, or secrets manager) to override this per environment.

## Feature Flags

| Flag Name | Default | Controlled By |
|-----------|---------|--------------|
| `ClientValidationEnabled` | `true` | `Web.config` `<appSettings>` |
| `UnobtrusiveJavaScriptEnabled` | `true` | `Web.config` `<appSettings>` |
| `webpages:Enabled` | `false` | `Web.config` `<appSettings>` — disables WebMatrix WebPages |
| `MvcBuildViews` | `false` | `ContosoUniversity.csproj` — disables Razor pre-compilation at build time |
| Role-based authorization filter | Disabled (commented out) | `FilterConfig.cs` — `AuthorizeAttribute` is commented out; no global auth enforcement |

> Note: No feature flag framework (LaunchDarkly, Unleash, .NET FeatureManagement, `@ConditionalOnProperty`, etc.) is used. The above entries are static boolean settings, not dynamic feature flags.

## Framework & Runtime Versions

| Component | Version | Source |
|-----------|---------|--------|
| Target Framework | .NET Framework 4.8 | `ContosoUniversity.csproj` (`TargetFrameworkVersion`) |
| ASP.NET MVC | 5.2.9 | `packages.config` (`Microsoft.AspNet.Mvc`) |
| ASP.NET Razor | 3.2.9 | `packages.config` (`Microsoft.AspNet.Razor`) |
| ASP.NET WebPages | 3.2.9 | `packages.config` (`Microsoft.AspNet.WebPages`) |
| ASP.NET Web Optimization (Bundling) | 1.1.3 | `packages.config` (`Microsoft.AspNet.Web.Optimization`) |
| Entity Framework Core | 3.1.32 | `packages.config` (`Microsoft.EntityFrameworkCore`) |
| EF Core SQL Server provider | 3.1.32 | `packages.config` (`Microsoft.EntityFrameworkCore.SqlServer`) |
| Microsoft.Data.SqlClient | 2.1.4 | `packages.config` |
| Microsoft.Extensions.DependencyInjection | 3.1.32 | `packages.config` |
| Microsoft.Extensions.Caching.Memory | 3.1.32 | `packages.config` |
| Microsoft.Extensions.Configuration | 3.1.32 | `packages.config` |
| Microsoft.Extensions.Logging | 3.1.32 | `packages.config` |
| Microsoft.Identity.Client (MSAL) | 4.21.1 | `packages.config` |
| Newtonsoft.Json | 13.0.3 | `packages.config` |
| Bootstrap (CSS/JS) | 5.3.3 | `packages.config` |
| jQuery | 3.7.1 | `packages.config` |
| jQuery Validation | 1.21.0 | `packages.config` |
| jQuery Unobtrusive Validation | 4.0.0 | `packages.config` (`Microsoft.jQuery.Unobtrusive.Validation`) |
| Modernizr | 2.6.2 | `packages.config` |
| WebGrease | 1.5.2 | `packages.config` |
| Antlr3 Runtime | 3.4.1.9004 | `packages.config` |
| NETStandard.Library | 2.0.3 | `packages.config` |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 2.0.1 | `packages.config` — Roslyn compiler support |
| MSBuild ToolsVersion | 15.0 | `ContosoUniversity.csproj` |
| Assembly version | 1.0.0.0 | `Properties/AssemblyInfo.cs` |
| Build tool | MSBuild (Visual Studio 2017+ toolchain) | `ContosoUniversity.csproj` |
