# 02.03-base-controller: Migrate BaseController from System.Web.Mvc to Microsoft.AspNetCore.Mvc

## Objective
Migrate BaseController to ASP.NET Core. This is the base for all other controllers — must complete before any derived controller migrations.

## Scope
- Controllers/BaseController.cs: change base class to Microsoft.AspNetCore.Mvc.Controller, update using directives, fix Dispose pattern (use IDisposable/IAsyncDisposable properly in Core), update constructor for DI injection of SchoolContext and NotificationService
- Remove SchoolContextFactory usage — inject SchoolContext via DI instead

## Done when
BaseController compiles with 0 errors using Microsoft.AspNetCore.Mvc.Controller as base class, DI-based injection for SchoolContext and NotificationService.
