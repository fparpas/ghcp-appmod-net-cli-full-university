using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ContosoUniversity.Services;
using ContosoUniversity.Models;
using ContosoUniversity.Data;

namespace ContosoUniversity.Controllers
{
    public abstract class BaseController : Controller
    {
        protected SchoolContext db;
        protected NotificationService notificationService;

        protected BaseController(SchoolContext db, NotificationService notificationService)
        {
            this.db = db;
            this.notificationService = notificationService;
        }

        protected Task SendEntityNotificationAsync(string entityType, string entityId, EntityOperation operation)
            => SendEntityNotificationAsync(entityType, entityId, null, operation);

        protected async Task SendEntityNotificationAsync(string entityType, string entityId, string entityDisplayName, EntityOperation operation)
        {
            try
            {
                var userName = "System";
                await notificationService.SendNotificationAsync(entityType, entityId, entityDisplayName, operation, userName)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
