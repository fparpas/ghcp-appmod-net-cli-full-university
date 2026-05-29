# Task 02.09 - Notifications Controller: Progress Details

## Completed: Migrated NotificationsController + Notification Views to ASP.NET Core MVC

### Files Modified
- **`Controllers/NotificationsController.cs`**
  - Changed `using System.Web.Mvc` → `using Microsoft.AspNetCore.Mvc`
  - Added DI constructor: `NotificationsController(SchoolContext, NotificationService)`
  - `JsonResult GetNotifications()` → `IActionResult GetNotifications()`
  - `JsonResult MarkAsRead()` → `IActionResult MarkAsRead()`
  - Removed `JsonRequestBehavior.AllowGet` (not needed in ASP.NET Core — GET returning JSON is allowed by default)
  - `ActionResult Index()` → `IActionResult Index()`

### Views (no changes needed)
- `Views/Notifications/Index.cshtml` — uses only JavaScript/AJAX, no server-side bundle references
