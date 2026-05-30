using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using ContosoUniversity.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Notification service backed by Azure Service Bus with Managed Identity (DefaultAzureCredential).
    /// Replaces the legacy System.Threading.Channels / in-memory implementation.
    /// Messages are sent to an Azure Service Bus queue and received via a background processor.
    /// </summary>
    public class NotificationService : IHostedService, IAsyncDisposable, IDisposable
    {
        private readonly ServiceBusSender _sender;
        private readonly ServiceBusProcessor _processor;
        private readonly ILogger<NotificationService> _logger;

        private readonly List<Notification> _allNotifications = new();
        private readonly ConcurrentQueue<Notification> _receivedQueue = new();
        private int _nextId = 1;

        private bool _disposed;

        /// <summary>
        /// Production constructor: reads namespace and queue name from configuration,
        /// authenticates via DefaultAzureCredential (Managed Identity).
        /// </summary>
        public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var fullyQualifiedNamespace = configuration["AzureServiceBus:FullyQualifiedNamespace"]
                ?? throw new InvalidOperationException("AzureServiceBus:FullyQualifiedNamespace is not configured.");
            var queueName = configuration["AzureServiceBus:QueueName"] ?? "contoso-university-notifications";

            var credential = new DefaultAzureCredential();
            var client = new ServiceBusClient(fullyQualifiedNamespace, credential);

            _sender = client.CreateSender(queueName);
            _processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

            RegisterProcessorHandlers();
        }

        /// <summary>
        /// Test constructor: accepts pre-built sender and processor for unit testing.
        /// </summary>
        internal NotificationService(
            ServiceBusSender sender,
            ServiceBusProcessor processor,
            ILogger<NotificationService> logger)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            RegisterProcessorHandlers();
        }

        // ── IHostedService ────────────────────────────────────────────────────────

        /// <summary>Starts the Service Bus message processor.</summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Azure Service Bus notification processor.");
            await _processor.StartProcessingAsync(cancellationToken);
        }

        /// <summary>Stops the Service Bus message processor.</summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Azure Service Bus notification processor.");
            await _processor.StopProcessingAsync(cancellationToken);
        }

        // ── Public API ────────────────────────────────────────────────────────────

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
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userName ?? "System",
                    IsRead = false
                };

                lock (_allNotifications)
                {
                    _allNotifications.Add(notification);
                }

                // Fire-and-forget send to Azure Service Bus
                _ = SendToServiceBusAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build or enqueue notification for {EntityType} {EntityId}.", entityType, entityId);
            }
        }

        /// <summary>
        /// Returns the next notification that the background processor has received
        /// from Azure Service Bus, or <c>null</c> if the inbox is empty.
        /// </summary>
        public Notification ReceiveNotification()
        {
            return _receivedQueue.TryDequeue(out var notification) ? notification : null;
        }

        /// <summary>Returns all sent notifications (read + unread), newest first.</summary>
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

        // ── Private helpers ───────────────────────────────────────────────────────

        private void RegisterProcessorHandlers()
        {
            _processor.ProcessMessageAsync += HandleMessageAsync;
            _processor.ProcessErrorAsync += HandleErrorAsync;
        }

        internal async Task HandleMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                var body = args.Message.Body.ToString();
                var notification = JsonSerializer.Deserialize<Notification>(body);

                if (notification != null)
                {
                    _receivedQueue.Enqueue(notification);
                    _logger.LogInformation(
                        "Received notification from Service Bus: {EntityType} {EntityId} {Operation}.",
                        notification.EntityType, notification.EntityId, notification.Operation);
                }

                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Service Bus message; abandoning.");
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception,
                "Azure Service Bus processor error. Source: {ErrorSource}, Entity: {EntityPath}.",
                args.ErrorSource, args.EntityPath);
            return Task.CompletedTask;
        }

        private async Task SendToServiceBusAsync(Notification notification)
        {
            try
            {
                var messageBody = JsonSerializer.Serialize(notification);
                var message = new ServiceBusMessage(messageBody);
                await _sender.SendMessageAsync(message);

                _logger.LogInformation(
                    "Sent notification to Service Bus: {EntityType} {EntityId} {Operation}.",
                    notification.EntityType, notification.EntityId, notification.Operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send notification to Azure Service Bus for {EntityType} {EntityId}.",
                    notification.EntityType, notification.EntityId);
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

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            try { await _sender.DisposeAsync(); } catch { /* best-effort */ }
            try { await _processor.DisposeAsync(); } catch { /* best-effort */ }
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
