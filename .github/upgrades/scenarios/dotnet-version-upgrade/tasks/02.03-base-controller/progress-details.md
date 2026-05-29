# Task 02.03 - Base Controller: Progress Details

## Completed: Migrated BaseController to ASP.NET Core MVC

### Files Modified
- **`Controllers/BaseController.cs`**
  - Changed `using System.Web.Mvc` → `using Microsoft.AspNetCore.Mvc`
  - Changed base class: `Controller` (now from `Microsoft.AspNetCore.Mvc`)
  - Removed direct instantiation of `SchoolContext` and `NotificationService`
  - Added DI constructor: `BaseController(SchoolContext context, NotificationService notificationSvc)`
  - Fields changed to `readonly` (injected, not created here)
  - Removed `Dispose(bool)` override — ASP.NET Core DI container manages lifetime; `SchoolContext` is scoped and disposed by the container, `NotificationService` is singleton and long-lived.
  - Removed `SchoolContextFactory` import (no longer needed in controllers)

### DI Strategy
- `SchoolContext` → registered as **Scoped** in `Program.cs` (per-request lifetime)
- `NotificationService` → registered as **Singleton** in `Program.cs` (in-memory channel persists across requests)
- All derived controllers must pass these via constructor parameters to `base(...)`
