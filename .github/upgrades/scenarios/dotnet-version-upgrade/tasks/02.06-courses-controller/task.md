# 02.06-courses-controller: Migrate CoursesController + Course views to ASP.NET Core MVC

## Objective
Migrate CoursesController and its views to ASP.NET Core.

## Key API Migrations
- HttpPostedFileBase → IFormFile
- HttpStatusCodeResult → BadRequest()/NotFound()
- SelectList usage → remains (Microsoft.AspNetCore.Mvc.Rendering.SelectList)

## Scope
- Controllers/CoursesController.cs
- Views/Courses/: all views

## Done when
CoursesController and all Course views compile with 0 errors.
