# Task 02.06 - Courses Controller: Progress Details

## Completed: Migrated CoursesController + Course Views to ASP.NET Core MVC

### Files Modified
- **`Controllers/CoursesController.cs`**
  - Added `using Microsoft.AspNetCore.Hosting`, `using Microsoft.AspNetCore.Http`, `using Microsoft.AspNetCore.Mvc.Rendering`
  - Removed `System.Web.Mvc`, `System.Web` imports
  - Added DI constructor: `CoursesController(SchoolContext, NotificationService, IWebHostEnvironment)`
  - All `ActionResult` → `IActionResult`
  - `BadRequest()`, `NotFound()` replacements applied
  - `HttpPostedFileBase teachingMaterialImage` → `IFormFile teachingMaterialImage`
  - `teachingMaterialImage.ContentLength` → `teachingMaterialImage.Length`
  - `teachingMaterialImage.SaveAs(path)` → `using (var stream = File.Create(path)) { teachingMaterialImage.CopyTo(stream); }`
  - `Server.MapPath("~/Uploads/...")` → `Path.Combine(_env.WebRootPath, "Uploads", ...)`
  - Virtual paths `~/Uploads/...` stored as `/Uploads/...` (web-root relative)
  - Old file deletion: converts `/Uploads/...` → `wwwroot/Uploads/...` via `_env.WebRootPath`
  - `new SelectList(...)` → now from `Microsoft.AspNetCore.Mvc.Rendering`

### Views Modified
- `Views/Courses/Create.cshtml` — `@Scripts.Render("~/bundles/jqueryval")` → direct script tags
- `Views/Courses/Edit.cshtml` — `@Scripts.Render("~/bundles/jqueryval")` → direct script tags
