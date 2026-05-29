# 02.09-notifications-controller: Migrate NotificationsController + Notification views to ASP.NET Core MVC

## Objective
Migrate NotificationsController and its views to ASP.NET Core.

## Key API Migrations
- Json(..., JsonRequestBehavior.AllowGet) → Json(...) (AllowGet not needed in Core)
- JsonResult → JsonResult (still works)

## Scope
- Controllers/NotificationsController.cs
- Views/Notifications/: all views

## Done when
NotificationsController and all Notification views compile with 0 errors.
