# 02.04-home-controller: Migrate HomeController + Home views to ASP.NET Core MVC

## Objective
Migrate HomeController and its views to ASP.NET Core.

## Scope
- Controllers/HomeController.cs: update using directives (System.Web.Mvc → Microsoft.AspNetCore.Mvc), ActionResult return types remain compatible, minor adjustments
- Views/Home/: review and update @Html.* helpers if needed

## Done when
HomeController and Home views compile with 0 errors.
