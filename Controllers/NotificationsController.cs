using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ContosoUniversity.Services;
using ContosoUniversity.Models;
using ContosoUniversity.Data;

namespace ContosoUniversity.Controllers
{
    public class NotificationsController : BaseController
    {
        public NotificationsController(SchoolContext db, NotificationService notificationService)
            : base(db, notificationService) { }

        // GET: api/notifications - Get pending notifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = new List<Notification>();

            try
            {
                Notification notification;
                while ((notification = await notificationService.ReceiveNotificationAsync()) != null)
                {
                    notifications.Add(notification);

                    if (notifications.Count >= 10)
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error retrieving notifications: " + ex.Message);
                return Json(new { success = false, message = "Error retrieving notifications" });
            }

            return Json(new {
                success = true,
                notifications = notifications,
                count = notifications.Count
            });
        }

        // POST: api/notifications/mark-read
        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            try
            {
                notificationService.MarkAsRead(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error marking notification as read: " + ex.Message);
                return Json(new { success = false, message = "Error updating notification" });
            }
        }

        // GET: Notifications/Index
        public IActionResult Index()
        {
            return View();
        }
    }
}
