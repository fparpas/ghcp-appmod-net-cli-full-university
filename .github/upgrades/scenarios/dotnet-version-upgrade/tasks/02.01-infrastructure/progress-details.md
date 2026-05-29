# Task 02.01 - Infrastructure: Progress Details

## Completed: Infrastructure Setup for ASP.NET Core Migration

### Files Created
- **`Program.cs`** — New ASP.NET Core entry point replacing `Global.asax`. Configures DI (SchoolContext, NotificationService), middleware (static files, routing, antiforgery), MVC controllers+views.
- **`appsettings.json`** — Connection string and app settings (replaces `web.config` appSettings and connectionStrings).
- **`appsettings.Development.json`** — Development-specific overrides (debug logging).
- **`Views/_ViewImports.cshtml`** — Adds tag helpers (`Microsoft.AspNetCore.Mvc.TagHelpers`) and common using directives for all views.
- **`wwwroot/css/`** — `Site.css`, `notifications.css` copied from `Content/`.
- **`wwwroot/js/`** — All JS files copied from `Scripts/` (jquery, bootstrap, validate).

### Files Modified
- **`ContosoUniversity.csproj`** — Target framework: `net48` → `net10.0`. Removed all legacy NuGet packages (System.Web, MSMQ, Optimization, jQuery NuGet packages). Added ASP.NET Core framework reference (`Microsoft.AspNetCore.App`). Packages: EFCore 10.0.8, Microsoft.Data.SqlClient 7.0.1, Newtonsoft.Json 13.0.4.
- **`Data/SchoolContextFactory.cs`** — Replaced `System.Configuration.ConfigurationManager` with `Microsoft.Extensions.Configuration` reading from `appsettings.json`. Still works for EF Core migrations tooling.
- **`Properties/AssemblyInfo.cs`** — Removed duplicate assembly attributes (Title, Version, FileVersion, Company, Product, Copyright, Configuration) that SDK-style projects auto-generate. Kept ComVisible and Guid.

### Files Deleted
- **`App_Start/BundleConfig.cs`** — Not needed; ASP.NET Core serves static files directly from wwwroot.
- **`App_Start/RouteConfig.cs`** — Routing moved to `Program.cs` via `MapDefaultControllerRoute()`.
- **`App_Start/FilterConfig.cs`** — Filters added globally in `Program.cs` via `AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))`.
- **`Global.asax`** / **`Global.asax.cs`** — Replaced entirely by `Program.cs`.
- **`Views/web.config`** — Not used in ASP.NET Core (Razor views are configured differently).
- **`Services/LoggingService.cs`** — File was empty; removed.

### Build Status
- **134 expected errors**: All from `System.Web.Mvc` (7 controllers) and `System.Messaging` (NotificationService).
- **0 infrastructure errors**: No errors from Program.cs, SchoolContextFactory, csproj, or any other infrastructure file.
- Controllers and NotificationService will be fixed in tasks 02.02–02.09.

### Notes
- Bootstrap CSS was not in the `Content/` folder (was a NuGet content package in old project). Will add Bootstrap via CDN link in `_Layout.cshtml` in task 02.10.
- `SchoolContextFactory.Create()` still called in `BaseController` — will be replaced with DI injection in task 02.03.
