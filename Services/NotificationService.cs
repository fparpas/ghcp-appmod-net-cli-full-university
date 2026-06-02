using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using ContosoUniversity.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Abstraction over the notification messaging layer for testability.
    /// </summary>
    public interface INotificationService
    {
        Task SendNotificationAsync(string entityType, string entityId, EntityOperation operation, string userName = null);
        Task SendNotificationAsync(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null);
        Task<Notification> ReceiveNotificationAsync();
        void MarkAsRead(int notificationId);
    }

    /// <summary>
    /// Sends and receives entity-change notifications via Azure Service Bus
    /// using Managed Identity (DefaultAzureCredential) — no connection strings or credentials.
    /// </summary>
    public class NotificationService : INotificationService, IDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;
        private readonly ServiceBusReceiver _receiver;
        private readonly ILogger<NotificationService> _logger;
        private int _nextId = 1;
        private bool _disposed;

        /// <summary>
        /// Production constructor — resolves configuration and authenticates via Managed Identity.
        /// </summary>
        public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var fullyQualifiedNamespace = configuration["AzureServiceBus:FullyQualifiedNamespace"]
                ?? throw new InvalidOperationException(
                    "AzureServiceBus:FullyQualifiedNamespace is not configured. " +
                    "Set it in appsettings.json or as an environment variable.");

            var queueName = configuration["AzureServiceBus:QueueName"] ?? "notifications";

            // Authenticate using Managed Identity — no connection strings or secrets
            var credential = new DefaultAzureCredential();
            _client = new ServiceBusClient(fullyQualifiedNamespace, credential);
            _sender = _client.CreateSender(queueName);
            _receiver = _client.CreateReceiver(queueName);
        }

        /// <summary>
        /// Test constructor — allows injecting mock sender and receiver directly.
        /// </summary>
        internal NotificationService(ServiceBusSender sender, ServiceBusReceiver receiver, ILogger<NotificationService> logger = null)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task SendNotificationAsync(string entityType, string entityId, EntityOperation operation, string userName = null)
            => SendNotificationAsync(entityType, entityId, null, operation, userName);

        /// <inheritdoc/>
        public async Task SendNotificationAsync(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)
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
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userName ?? "System",
                    IsRead = false
                };

                var messageBody = JsonSerializer.Serialize(notification);
                var serviceBusMessage = new ServiceBusMessage(messageBody);
                await _sender.SendMessageAsync(serviceBusMessage).ConfigureAwait(false);

                _logger?.LogInformation(
                    "Notification sent for {EntityType} {EntityId} — {Operation}",
                    entityType, entityId, operation);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send notification for {EntityType} {EntityId}", entityType, entityId);
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<Notification> ReceiveNotificationAsync()
        {
            try
            {
                // Short timeout so polling loops remain responsive when the queue is empty
                var receivedMessage = await _receiver
                    .ReceiveMessageAsync(TimeSpan.FromMilliseconds(200))
                    .ConfigureAwait(false);

                if (receivedMessage == null)
                    return null;

                var body = receivedMessage.Body.ToString();
                var notification = JsonSerializer.Deserialize<Notification>(body);

                // Complete the message to remove it from the queue
                await _receiver.CompleteMessageAsync(receivedMessage).ConfigureAwait(false);

                return notification;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to receive notification");
                System.Diagnostics.Debug.WriteLine($"Failed to receive notification: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc/>
        public void MarkAsRead(int notificationId)
        {
            // Azure Service Bus messages are completed (removed from queue) upon receipt.
            // No further action is needed to mark them as read.
        }

        // ── Internal helpers ────────────────────────────────────────────────────

        internal static string GenerateMessage(string entityType, string entityId, string entityDisplayName, EntityOperation operation)
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

        // ── IDisposable ─────────────────────────────────────────────────────────

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _sender?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _receiver?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _client?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            _disposed = true;
        }
    }
}
