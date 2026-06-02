using System;
using System.Collections.Concurrent;
using System.Threading;
using ContosoUniversity.Models;

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly ConcurrentQueue<Notification> _queue = new ConcurrentQueue<Notification>();
        private int _nextId = 1;

        public void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)
        {
            try
            {
                var notification = new Notification
                {
                    Id = Interlocked.Increment(ref _nextId),
                    EntityType = entityType,
                    EntityId = entityId,
                    Operation = operation.ToString(),
                    Message = GenerateMessage(entityType, entityId, entityDisplayName, operation),
                    CreatedAt = DateTime.Now,
                    CreatedBy = userName ?? "System",
                    IsRead = false
                };

                _queue.Enqueue(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        public Notification ReceiveNotification()
        {
            if (_queue.TryDequeue(out var notification))
                return notification;
            return null;
        }

        public void MarkAsRead(int notificationId)
        {
            // In-memory implementation - notifications are dequeued on receive
        }

        private string GenerateMessage(string entityType, string entityId, string entityDisplayName, EntityOperation operation)
        {
            var displayText = !string.IsNullOrWhiteSpace(entityDisplayName)
                ? entityType + " '" + entityDisplayName + "'"
                : entityType + " (ID: " + entityId + ")";

            switch (operation)
            {
                case EntityOperation.CREATE:
                    return $"New {displayText} has been created";
                case EntityOperation.UPDATE:
                    return $"{displayText} has been updated";
                case EntityOperation.DELETE:
                    return $"{displayText} has been deleted";
                default:
                    return $"{displayText} operation: {operation}";
            }
        }

        public void Dispose()
        {
            // Nothing to dispose in in-memory implementation
        }
    }
}
