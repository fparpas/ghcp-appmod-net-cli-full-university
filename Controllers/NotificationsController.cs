using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ContosoUniversity.Services;
using ContosoUniversity.Models;
using ContosoUniversity.Data;

namespace ContosoUniversity.Controllers
{
    public class NotificationsController : BaseController
    {
        public NotificationsController(
            SchoolContext context,
            NotificationService notificationSvc,
            ILogger<BaseController> logger)
            : base(context, notificationSvc, logger) { }

        // GET: api/notifications - Get pending notifications for admin
        [HttpGet]
        public IActionResult GetNotifications()
        {
            var notifications = new List<Notification>();

            try
            {
                // Read all available notifications from the channel
                Notification notification;
                while ((notification = notificationService.ReceiveNotification()) != null)
                {
                    notifications.Add(notification);

                    // Limit to prevent overwhelming the UI
                    if (notifications.Count >= 10)
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications: {Message}", ex.Message);
                return Json(new { success = false, message = "Error retrieving notifications" });
            }

            return Json(new
            {
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
                _logger.LogError(ex, "Error marking notification {NotificationId} as read: {Message}", id, ex.Message);
                return Json(new { success = false, message = "Error updating notification" });
            }
        }

        // GET: Notifications/Index - Notification dashboard
        public IActionResult Index()
        {
            return View();
        }
    }
}
