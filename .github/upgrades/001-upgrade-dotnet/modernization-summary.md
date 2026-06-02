# Modernization Summary: Upgrade to .NET 10 (net10.0)

## Task
**TaskId:** 001-upgrade-dotnet  
**Description:** Upgrade ContosoUniversity from .NET Framework 4.8 to .NET 10 (net10.0), converting the project to SDK-style and migrating all ASP.NET Framework APIs to ASP.NET Core.

## Outcome
✅ Build succeeded — 0 errors, 0 warnings  
✅ Project targets `net10.0`  
✅ All ASP.NET Framework (System.Web) APIs removed  
✅ All incompatible NuGet packages replaced  

---

## Changes Made

### 1. Project File — `ContosoUniversity.csproj`
- **Before:** Legacy non-SDK-style csproj (`ToolsVersion="15.0"`) targeting `.NET Framework 4.8`
- **After:** SDK-style `<Project Sdk="Microsoft.NET.Sdk.Web">` targeting `net10.0`
- Removed all `<Reference>` items pointing to `System.Web.*`, `System.Messaging`, `Antlr`, `WebGrease`, `Microsoft.AspNet.*` packages
- Added `PackageReference` for:
  - `Microsoft.EntityFrameworkCore.SqlServer` v10.0.8
  - `Microsoft.EntityFrameworkCore.Design` v10.0.8
- Set `GenerateAssemblyInfo=false` (retains Properties/AssemblyInfo.cs)

### 2. Application Startup — `Program.cs` (new)
- Created new `Program.cs` as ASP.NET Core minimal hosting entry point
- Replaced `Global.asax.cs` application lifecycle hooks with:
  - `WebApplication.CreateBuilder(args)` 
  - `AddControllersWithViews()` for MVC
  - `AddDbContext<SchoolContext>()` for EF Core via DI
  - `AddSingleton<NotificationService>()` for in-memory notification queue
  - `UseStaticFiles()` middleware with additional providers for `Content/`, `Scripts/`, and `Uploads/` folders
  - `MapControllerRoute()` replacing `RouteConfig.RegisterRoutes(RouteTable.Routes)`
  - Database seeding via scoped `DbInitializer.Initialize(context)`

### 3. Configuration — `appsettings.json` (new)
- Created `appsettings.json` to replace `Web.config`
- Connection string `DefaultConnection` migrated from `<connectionStrings>` to `ConnectionStrings` JSON section
- All `<appSettings>` (webpages:Version, bundling keys, MSMQ queue path) removed as no longer applicable

### 4. Views Import — `Views/_ViewImports.cshtml` (new)
- Created `_ViewImports.cshtml` replacing `Views/Web.config` (Razor host configuration)
- Added `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` for ASP.NET Core tag helpers
- Added namespace imports for ContosoUniversity, Models, SchoolViewModels

### 5. Controllers — Migrated all from `System.Web.Mvc` to `Microsoft.AspNetCore.Mvc`

#### BaseController.cs
- Removed `SchoolContextFactory.Create()` (old: manual factory pattern)
- Added DI constructor `(SchoolContext db, NotificationService notificationService)`
- `using System.Web.Mvc` → `using Microsoft.AspNetCore.Mvc`

#### HomeController.cs
- DI constructor added
- `ActionResult` → `IActionResult`
- Renamed `Unauthorized()` → `AccessDenied()` (avoids hiding `ControllerBase.Unauthorized()`)

#### StudentsController.cs
- DI constructor added
- `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`
- `HttpNotFound()` → `NotFound()`
- `[Bind(Include = "...")]` → `[Bind("...")]` (ASP.NET Core syntax)
- Removed `using System.Net`

#### CoursesController.cs
- DI constructor added; injected `IWebHostEnvironment`
- `HttpPostedFileBase teachingMaterialImage` → `IFormFile teachingMaterialImage`
- `teachingMaterialImage.ContentLength` → `teachingMaterialImage.Length`
- `Server.MapPath("~/Uploads/...")` → `Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads", ...)`
- `teachingMaterialImage.SaveAs(filePath)` → `CopyTo(FileStream)` 
- Removed `using System.Web`

#### InstructorsController.cs
- DI constructor added
- `TryUpdateModel(...)` → `await TryUpdateModelAsync<Instructor>(...)` (made Edit POST action `async Task<IActionResult>`)
- `new HttpStatusCodeResult(...)` → `BadRequest()` / `NotFound()`

#### DepartmentsController.cs
- DI constructor added
- `new HttpStatusCodeResult(...)` → `BadRequest()` / `NotFound()`

#### NotificationsController.cs
- DI constructor added
- `Json(..., JsonRequestBehavior.AllowGet)` → `Json(...)` (AllowGet not needed in ASP.NET Core)

### 6. NotificationService.cs — MSMQ Replaced
- **Before:** Used `System.Messaging.MessageQueue` (MSMQ) — not supported on .NET Core
- **After:** In-memory `ConcurrentQueue<Notification>` — thread-safe, no external dependencies
- Removed `Newtonsoft.Json` dependency (no longer needed for queue serialization)
- `SendNotification()`, `ReceiveNotification()`, `MarkAsRead()` signatures preserved

### 7. SchoolContextFactory.cs
- Removed `System.Configuration.ConfigurationManager` dependency
- Factory now accepts `connectionString` parameter (retained for completeness; primary DI path via `AddDbContext`)

### 8. Views/Shared/_Layout.cshtml
- Replaced `@Styles.Render("~/Content/css")` with direct `<link>` tags
- Replaced `@Scripts.Render("~/bundles/modernizr")` with `<script src="~/Scripts/modernizr-2.6.2.js">`
- Replaced `@Scripts.Render("~/bundles/jquery")` with `<script src="~/Scripts/jquery-3.4.1.min.js">`
- Replaced `@Scripts.Render("~/bundles/bootstrap")` with direct script tags

### 9. Views — All views with `@Scripts.Render("~/bundles/jqueryval")`
Replaced in: Students/Create, Students/Edit, Courses/Create, Courses/Edit, Departments/Create, Departments/Edit, Instructors/Create, Instructors/Edit

### 10. Views/Shared/Error.cshtml
- Removed `@model System.Web.Mvc.HandleErrorInfo` (System.Web type)
- Replaced with simple static error message view

### 11. App_Start Files
- `RouteConfig.cs` — replaced with stub comment (routing moved to `Program.cs`)
- `BundleConfig.cs` — replaced with stub comment (bundling removed)
- `FilterConfig.cs` — replaced with stub comment (filter registration moved to `Program.cs`)

### 12. Global.asax.cs
- Replaced with stub comment (startup moved to `Program.cs`)

### 13. Deleted Files
- `Views/Web.config` — Razor v3 configuration, replaced by `_ViewImports.cshtml`
- `packages.config` — replaced by `PackageReference` in SDK-style csproj

---

## Issues Addressed
- Project file needs to be converted to SDK-style ✅
- Project's target framework needs to be changed to net10.0 ✅
- NuGet packages incompatible (Microsoft.AspNet.Mvc, WebGrease, Antlr, etc.) ✅ removed
- NuGet package functionality included with framework reference (EF Core, Extensions) ✅ updated
- ASP.NET Framework (System.Web) ✅ fully removed
- Routes registration via RouteCollection → ASP.NET Core endpoint routing ✅
- Global.asax.cs → Program.cs ✅
- System.Web.Optimization bundling → direct static file references ✅
- MSMQ (System.Messaging) → in-memory ConcurrentQueue ✅
- Legacy Configuration System (ConfigurationManager/Web.config) → appsettings.json ✅
- Binding redirect conflicts → not applicable in SDK-style net10.0 ✅
- Static content served via UseStaticFiles middleware ✅
