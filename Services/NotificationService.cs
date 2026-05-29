using System;
using System.Collections.Generic;
using System.Threading.Channels;
using ContosoUniversity.Models;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// In-memory notification service backed by System.Threading.Channels.
    /// Replaces the legacy System.Messaging / MSMQ implementation — MSMQ has no .NET Core equivalent.
    /// For this demo app, an in-memory bounded channel provides equivalent send/receive semantics.
    /// </summary>
    public class NotificationService : IDisposable
    {
        private readonly Channel<Notification> _channel;
        private readonly List<Notification> _allNotifications = new();
        private int _nextId = 1;

        public NotificationService()
        {
            // Bounded channel: drop oldest if full (same semantics as MSMQ with a queue limit)
            _channel = Channel.CreateBounded<Notification>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false,
                SingleWriter = false
            });
        }

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
                    Id = _nextId++,
                    EntityType = entityType,
                    EntityId = entityId,
                    Operation = operation.ToString(),
                    Message = GenerateMessage(entityType, entityId, entityDisplayName, operation),
                    CreatedAt = DateTime.Now,
                    CreatedBy = userName ?? "System",
                    IsRead = false
                };

                // Keep in-memory list for retrieval
                lock (_allNotifications)
                {
                    _allNotifications.Add(notification);
                }

                // Also write to channel (fire-and-forget; TryWrite is non-blocking)
                _channel.Writer.TryWrite(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        public Notification ReceiveNotification()
        {
            try
            {
                if (_channel.Reader.TryRead(out var notification))
                    return notification;
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to receive notification: {ex.Message}");
                return null;
            }
        }

        /// <summary>Returns all notifications (read + unread), newest first.</summary>
        public IReadOnlyList<Notification> GetAllNotifications()
        {
            lock (_allNotifications)
            {
                var copy = new List<Notification>(_allNotifications);
                copy.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
                return copy;
            }
        }

        public void MarkAsRead(int notificationId)
        {
            lock (_allNotifications)
            {
                var notification = _allNotifications.Find(n => n.Id == notificationId);
                if (notification != null)
                    notification.IsRead = true;
            }
        }

        private static string GenerateMessage(string entityType, string entityId, string entityDisplayName, EntityOperation operation)
        {
            var displayText = !string.IsNullOrWhiteSpace(entityDisplayName)
                ? $"{entityType} '{entityDisplayName}'"
                : $"{entityType} (ID: {entityId})";

            return operation switch
            {
                EntityOperation.CREATE => $"New {displayText} has been created",
                EntityOperation.UPDATE => $"{displayText} has been updated",
                EntityOperation.DELETE => $"{displayText} has been deleted",
                _ => $"{displayText} operation: {operation}"
            };
        }

        public void Dispose()
        {
            _channel.Writer.TryComplete();
        }
    }
}

