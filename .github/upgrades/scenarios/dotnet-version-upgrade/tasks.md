# Migration Progress

**Progress**: 5/12 tasks complete <progress value="42" max="100"></progress> 42%
**Status**: In Progress - Task 02-upgrade

## Tasks

- ✅ 01-sdk-style: Convert ContosoUniversity to SDK-style project format ([Content](tasks/01-sdk-style/task.md), [Progress](tasks/01-sdk-style/progress-details.md))
- 🔄 02-upgrade: Upgrade ContosoUniversity to net10.0 and migrate to ASP.NET Core MVC ([Content](tasks/02-upgrade/task.md))
   - ✅ 02.01-infrastructure: Update TFM to net10.0, update packages, create Program.cs and appsettings.json, set up wwwroot, remove App_Start and Global.asax ([Content](tasks/02.01-infrastructure/task.md), [Progress](tasks/02.01-infrastructure/progress-details.md))
   - ✅ 02.02-notification-service: Replace System.Messaging (MSMQ) with an in-memory Channel<T> implementation ([Content](tasks/02.02-notification-service/task.md), [Progress](tasks/02.02-notification-service/progress-details.md))
   - ✅ 02.03-base-controller: Migrate BaseController from System.Web.Mvc to Microsoft.AspNetCore.Mvc ([Content](tasks/02.03-base-controller/task.md), [Progress](tasks/02.03-base-controller/progress-details.md))
   - ✅ 02.04-home-controller: Migrate HomeController + Home views to ASP.NET Core MVC ([Content](tasks/02.04-home-controller/task.md), [Progress](tasks/02.04-home-controller/progress-details.md))
   - 🔄 02.05-students-controller: Migrate StudentsController + Student views to ASP.NET Core MVC ([Content](tasks/02.05-students-controller/task.md))
   - 🔲 02.06-courses-controller: Migrate CoursesController + Course views to ASP.NET Core MVC
   - 🔲 02.07-departments-controller: Migrate DepartmentsController + Department views to ASP.NET Core MVC
   - 🔲 02.08-instructors-controller: Migrate InstructorsController + Instructor views to ASP.NET Core MVC
   - 🔲 02.09-notifications-controller: Migrate NotificationsController + Notification views to ASP.NET Core MVC
   - 🔲 02.10-layout-and-shared-views: Migrate _Layout.cshtml and shared views from MVC 5 to ASP.NET Core
- 🔲 03-validation: Final validation and cleanup

**Legend**: ✅ Complete | 🔄 In Progress | 🔲 Pending | ⚠️ Blocked | ❌ Failed
