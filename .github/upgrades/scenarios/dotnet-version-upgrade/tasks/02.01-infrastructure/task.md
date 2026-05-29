# 02.01-infrastructure: Update TFM to net10.0, update packages, create Program.cs and appsettings.json, set up wwwroot, remove App_Start and Global.asax

## Objective
Update the project infrastructure: TFM to net10.0, package updates, ASP.NET Core bootstrap (Program.cs), configuration migration (web.config → appsettings.json), static assets (wwwroot), and removal of App_Start files.

## Scope
- ContosoUniversity.csproj: change TargetFramework net48→net10.0, remove incompatible packages (Microsoft.AspNet.Mvc, Microsoft.AspNet.Razor, Microsoft.AspNet.WebPages, Microsoft.AspNet.Web.Optimization, WebGrease, Antlr, Microsoft.Web.Infrastructure, Microsoft.CodeDom.Providers.DotNetCompilerPlatform, NETStandard.Library, System.Buffers, System.Memory, System.Numerics.Vectors, System.Threading.Tasks.Extensions, System.ComponentModel.Annotations, Microsoft.Data.SqlClient.SNI.runtime, Microsoft.Bcl.HashCode, Microsoft.Bcl.AsyncInterfaces), upgrade EF Core packages to 10.0.x, fix security vulnerability (Microsoft.Data.SqlClient → 7.0.1), upgrade Microsoft.Extensions.* to 10.0.x, update Newtonsoft.Json to 13.0.4, remove deprecated Microsoft.Identity.Client, add Microsoft.AspNetCore.App framework reference
- Create Program.cs with WebApplication builder, DI registration for SchoolContext and NotificationService, ASP.NET Core routing, static file middleware, error handling middleware
- Create appsettings.json with connection string and NotificationQueuePath
- Create wwwroot/ folder structure, move Content/ and Scripts/ files there
- Remove App_Start/ folder (BundleConfig.cs, RouteConfig.cs, FilterConfig.cs)
- Remove Global.asax and Global.asax.cs
- Remove CopySQLClientNativeBinaries build target (now handled by SDK)
- Remove the old web.config assembly binding redirects
- Update Views/web.config and Views/_ViewStart.cshtml for ASP.NET Core
- Update SchoolContextFactory.cs to use IConfiguration instead of ConfigurationManager

## Done when
Project targets net10.0, builds with 0 errors (excluding controller compilation errors from remaining System.Web references), Program.cs exists with working ASP.NET Core setup, appsettings.json contains connection string, wwwroot has static files.
