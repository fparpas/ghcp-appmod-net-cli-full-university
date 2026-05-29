# Task 02.04 - Home Controller: Progress Details

## Completed: Migrated HomeController + Home Views to ASP.NET Core MVC

### Files Modified
- **`Controllers/HomeController.cs`**
  - Changed `using System.Web.Mvc` → `using Microsoft.AspNetCore.Mvc`
  - Added `using ContosoUniversity.Services`
  - Added DI constructor: `HomeController(SchoolContext, NotificationService)` → passes to `base(...)`
  - Changed all `ActionResult` returns to `IActionResult`

### Views (no changes required)
- `Views/Home/Index.cshtml` — Uses only `@Url.Action`, `ViewBag`, and HTML — compatible as-is
- `Views/Home/About.cshtml` — Uses `@Html.DisplayFor`, `@foreach` — compatible as-is
- `Views/Home/Contact.cshtml` — Uses only `ViewBag` — compatible as-is
