using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ContosoUniversity.Models;
using ContosoUniversity.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ContosoUniversity.Tests.Services
{
    public class NotificationServiceTests : IDisposable
    {
        private readonly FakeServiceBusSender _fakeSender;
        private readonly FakeServiceBusProcessor _fakeProcessor;
        private readonly Mock<ServiceBusReceiver> _mockReceiver;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _fakeSender = new FakeServiceBusSender();
            _fakeProcessor = new FakeServiceBusProcessor();

            _mockReceiver = new Mock<ServiceBusReceiver>();
            _mockReceiver
                .Setup(r => r.CompleteMessageAsync(
                    It.IsAny<ServiceBusReceivedMessage>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _service = new NotificationService(
                _fakeSender,
                _fakeProcessor,
                NullLogger<NotificationService>.Instance);
        }

        public void Dispose() => _service.Dispose();

        // ── SendNotification ──────────────────────────────────────────────────

        [Fact]
        public async Task SendNotification_AddsToAllNotifications()
        {
            _service.SendNotification("Student", "1", EntityOperation.CREATE, "testuser");
            await _fakeSender.WaitForMessageAsync(TimeSpan.FromSeconds(2));

            var notifications = _service.GetAllNotifications();

            Assert.Single(notifications);
            Assert.Equal("Student", notifications[0].EntityType);
            Assert.Equal("1", notifications[0].EntityId);
            Assert.Equal("CREATE", notifications[0].Operation);
            Assert.Equal("testuser", notifications[0].CreatedBy);
            Assert.False(notifications[0].IsRead);
        }

        [Fact]
        public async Task SendNotification_SendsMessageToServiceBus()
        {
            _service.SendNotification("Course", "10", EntityOperation.UPDATE);
            await _fakeSender.WaitForMessageAsync(TimeSpan.FromSeconds(2));

            Assert.Single(_fakeSender.SentMessages);
        }

        [Fact]
        public async Task SendNotification_MessageBodyContainsCorrectFields()
        {
            _service.SendNotification("Department", "5", "Engineering", EntityOperation.DELETE, "admin");
            await _fakeSender.WaitForMessageAsync(TimeSpan.FromSeconds(2));

            Assert.Single(_fakeSender.SentMessages);

            var sentMessage = _fakeSender.SentMessages[0];
            var body = sentMessage.Body.ToString();
            var notification = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);

            Assert.NotNull(notification);
            Assert.Equal("Department", notification["EntityType"].GetString());
            Assert.Equal("5", notification["EntityId"].GetString());
            Assert.Equal("DELETE", notification["Operation"].GetString());
            Assert.Equal("admin", notification["CreatedBy"].GetString());
        }

        [Fact]
        public async Task SendNotification_WithDisplayName_UsesDisplayNameInMessage()
        {
            _service.SendNotification("Student", "42", "John Doe", EntityOperation.CREATE);
            await _fakeSender.WaitForMessageAsync(TimeSpan.FromSeconds(2));

            var notifications = _service.GetAllNotifications();

            Assert.Single(notifications);
            Assert.Contains("John Doe", notifications[0].Message);
            Assert.Contains("created", notifications[0].Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendNotification_WithoutDisplayName_UsesEntityIdInMessage()
        {
            _service.SendNotification("Course", "99", null, EntityOperation.UPDATE);
            await _fakeSender.WaitForMessageAsync(TimeSpan.FromSeconds(2));

            var notifications = _service.GetAllNotifications();

            Assert.Single(notifications);
            Assert.Contains("99", notifications[0].Message);
            Assert.Contains("updated", notifications[0].Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(EntityOperation.CREATE, "created")]
        [InlineData(EntityOperation.UPDATE, "updated")]
        [InlineData(EntityOperation.DELETE, "deleted")]
        public async Task SendNotification_OperationTypes_ProduceCorrectMessages(EntityOperation operation, string expectedWord)
        {
            _service.SendNotification("Instructor", "7", "Prof. Smith", operation);
            await _fakeSender.WaitForMessageAsync(TimeSpan.FromSeconds(2));

            var notifications = _service.GetAllNotifications();

            Assert.Single(notifications);
            Assert.Contains(expectedWord, notifications[0].Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendNotification_DefaultsToSystemUser_WhenUserNameIsNull()
        {
            _service.SendNotification("Student", "1", EntityOperation.CREATE);
            await _fakeSender.WaitForMessageAsync(TimeSpan.FromSeconds(2));

            var notifications = _service.GetAllNotifications();

            Assert.Equal("System", notifications[0].CreatedBy);
        }

        [Fact]
        public async Task SendNotification_MultipleNotifications_AssignsIncrementingIds()
        {
            _service.SendNotification("Student", "1", EntityOperation.CREATE);
            _service.SendNotification("Student", "2", EntityOperation.UPDATE);
            _service.SendNotification("Student", "3", EntityOperation.DELETE);

            await _fakeSender.WaitForMessageCountAsync(3, TimeSpan.FromSeconds(2));

            var notifications = _service.GetAllNotifications();

            Assert.Equal(3, notifications.Count);
            var ids = new HashSet<int>(notifications.Select(n => n.Id));
            Assert.Equal(3, ids.Count); // all unique
        }

        // ── ReceiveNotification ───────────────────────────────────────────────

        [Fact]
        public void ReceiveNotification_ReturnsNullWhenQueueIsEmpty()
        {
            var result = _service.ReceiveNotification();

            Assert.Null(result);
        }

        [Fact]
        public async Task ReceiveNotification_ReturnsNotificationAfterProcessorDeliversMessage()
        {
            var payload = new Notification
            {
                Id = 1,
                EntityType = "Student",
                EntityId = "42",
                Operation = "CREATE",
                Message = "New Student (ID: 42) has been created",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                IsRead = false
            };

            await SimulateServiceBusMessageAsync(payload);

            var received = _service.ReceiveNotification();

            Assert.NotNull(received);
            Assert.Equal("Student", received.EntityType);
            Assert.Equal("42", received.EntityId);
            Assert.Equal("CREATE", received.Operation);
        }

        [Fact]
        public async Task ReceiveNotification_ReturnsNullAfterQueueDrained()
        {
            var payload = new Notification
            {
                Id = 1,
                EntityType = "Course",
                EntityId = "10",
                Operation = "UPDATE",
                Message = "Course (ID: 10) has been updated",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                IsRead = false
            };

            await SimulateServiceBusMessageAsync(payload);

            // First call drains the one message
            _service.ReceiveNotification();

            // Second call should return null
            var result = _service.ReceiveNotification();

            Assert.Null(result);
        }

        [Fact]
        public async Task ReceiveNotification_CanReceiveMultipleMessages()
        {
            for (int i = 1; i <= 3; i++)
            {
                await SimulateServiceBusMessageAsync(new Notification
                {
                    Id = i,
                    EntityType = "Student",
                    EntityId = i.ToString(),
                    Operation = "CREATE",
                    Message = $"New Student (ID: {i}) has been created",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    IsRead = false
                });
            }

            var received = new List<Notification>();
            Notification n;
            while ((n = _service.ReceiveNotification()) != null)
                received.Add(n);

            Assert.Equal(3, received.Count);
        }

        // ── GetAllNotifications ───────────────────────────────────────────────

        [Fact]
        public void GetAllNotifications_ReturnsEmptyListInitially()
        {
            var notifications = _service.GetAllNotifications();

            Assert.Empty(notifications);
        }

        [Fact]
        public async Task GetAllNotifications_ReturnsNewestFirst()
        {
            _service.SendNotification("Student", "1", EntityOperation.CREATE);
            await Task.Delay(5);
            _service.SendNotification("Student", "2", EntityOperation.UPDATE);
            await Task.Delay(5);
            _service.SendNotification("Student", "3", EntityOperation.DELETE);

            // Allow fire-and-forget tasks to complete
            await _fakeSender.WaitForMessageCountAsync(3, TimeSpan.FromSeconds(2));

            var notifications = _service.GetAllNotifications();

            Assert.Equal(3, notifications.Count);
            // Newest (CreatedAt desc) should be first – verify the list is sorted descending
            Assert.True(notifications[0].CreatedAt >= notifications[1].CreatedAt);
            Assert.True(notifications[1].CreatedAt >= notifications[2].CreatedAt);
        }

        // ── MarkAsRead ────────────────────────────────────────────────────────

        [Fact]
        public async Task MarkAsRead_SetsIsReadToTrue()
        {
            _service.SendNotification("Student", "1", EntityOperation.CREATE);
            await _fakeSender.WaitForMessageAsync(TimeSpan.FromSeconds(2));

            var notification = _service.GetAllNotifications()[0];
            Assert.False(notification.IsRead);

            _service.MarkAsRead(notification.Id);

            var updated = _service.GetAllNotifications()[0];
            Assert.True(updated.IsRead);
        }

        [Fact]
        public async Task MarkAsRead_NonExistentId_DoesNotThrow()
        {
            _service.SendNotification("Student", "1", EntityOperation.CREATE);
            await _fakeSender.WaitForMessageAsync(TimeSpan.FromSeconds(2));

            // Should not throw for unknown ID
            var ex = Record.Exception(() => _service.MarkAsRead(99999));
            Assert.Null(ex);
        }

        [Fact]
        public async Task MarkAsRead_OnlyMarksSpecifiedNotification()
        {
            _service.SendNotification("Student", "1", EntityOperation.CREATE);
            _service.SendNotification("Student", "2", EntityOperation.UPDATE);
            await _fakeSender.WaitForMessageCountAsync(2, TimeSpan.FromSeconds(2));

            var all = _service.GetAllNotifications();
            _service.MarkAsRead(all[0].Id); // Mark only the first (newest)

            var afterMark = _service.GetAllNotifications();
            int readCount = afterMark.Count(n => n.IsRead);
            Assert.Equal(1, readCount);
        }

        // ── IHostedService lifecycle ──────────────────────────────────────────

        [Fact]
        public async Task StartAsync_StartsTheProcessor()
        {
            await _service.StartAsync(CancellationToken.None);

            Assert.True(_fakeProcessor.StartProcessingCalled);
        }

        [Fact]
        public async Task StopAsync_StopsTheProcessor()
        {
            await _service.StartAsync(CancellationToken.None);
            await _service.StopAsync(CancellationToken.None);

            Assert.True(_fakeProcessor.StopProcessingCalled);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task SimulateServiceBusMessageAsync(Notification notification)
        {
            var json = JsonSerializer.Serialize(notification);
            var testMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString(json),
                messageId: Guid.NewGuid().ToString());

            var args = new ProcessMessageEventArgs(
                testMessage,
                _mockReceiver.Object,
                CancellationToken.None);

            // Invoke the internal message handler directly (bypasses real Service Bus transport)
            await _service.HandleMessageAsync(args);
        }
    }

    // ── Test doubles ─────────────────────────────────────────────────────────

    /// <summary>Fake sender that records messages without network I/O.</summary>
    internal class FakeServiceBusSender : ServiceBusSender
    {
        public List<ServiceBusMessage> SentMessages { get; } = new();
        private int _messageCount;
        private readonly TaskCompletionSource<bool> _firstMessageTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
        {
            SentMessages.Add(message);
            Interlocked.Increment(ref _messageCount);
            _firstMessageTcs.TrySetResult(true);
            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <summary>Waits until at least one message has been sent, or the timeout expires.</summary>
        public async Task WaitForMessageAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            try { await _firstMessageTcs.Task.WaitAsync(cts.Token); }
            catch (OperationCanceledException) { /* best-effort; test assertion will fail if needed */ }
        }

        /// <summary>Polls until the expected message count is reached or timeout expires.</summary>
        public async Task WaitForMessageCountAsync(int expectedCount, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (_messageCount < expectedCount && DateTime.UtcNow < deadline)
                await Task.Delay(10);
        }
    }

    /// <summary>Fake processor that tracks lifecycle calls without any real Service Bus connection.</summary>
    internal class FakeServiceBusProcessor : ServiceBusProcessor
    {
        public bool StartProcessingCalled { get; private set; }
        public bool StopProcessingCalled { get; private set; }

        public override Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            StartProcessingCalled = true;
            return Task.CompletedTask;
        }

        public override Task StopProcessingAsync(CancellationToken cancellationToken = default)
        {
            StopProcessingCalled = true;
            return Task.CompletedTask;
        }
    }
}
