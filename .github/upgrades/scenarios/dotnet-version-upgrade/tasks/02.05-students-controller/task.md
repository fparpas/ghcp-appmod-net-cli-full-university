# 02.05-students-controller: Migrate StudentsController + Student views to ASP.NET Core MVC

## Objective
Migrate StudentsController and its views to ASP.NET Core.

## Key API Migrations
- HttpStatusCodeResult(HttpStatusCode.BadRequest) → BadRequest()
- HttpNotFound() → NotFound()
- [Bind(Include=...)] → [Bind] or remove (ASP.NET Core uses model binding differently)
- db.Students.Find(id) → db.Students.FindAsync(id)
- RedirectToAction → remains the same

## Scope
- Controllers/StudentsController.cs
- Views/Students/: all views

## Done when
StudentsController and all Student views compile with 0 errors.
