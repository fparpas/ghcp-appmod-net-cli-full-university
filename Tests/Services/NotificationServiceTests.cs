using System;
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
    /// <summary>
    /// Unit tests for <see cref="NotificationService"/>.
    ///
    /// The tests use Moq to mock <see cref="ServiceBusSender"/> and
    /// <see cref="ServiceBusReceiver"/> via their virtual members, exercising
    /// the internal test constructor so no real Azure connection is required.
    /// </summary>
    public class NotificationServiceTests
    {
        // ── GenerateMessage (static helper) ────────────────────────────────────

        [Theory]
        [InlineData("Student", "42", "Jane Doe", EntityOperation.CREATE,
            "New Student 'Jane Doe' has been created")]
        [InlineData("Student", "42", "Jane Doe", EntityOperation.UPDATE,
            "Student 'Jane Doe' has been updated")]
        [InlineData("Student", "42", "Jane Doe", EntityOperation.DELETE,
            "Student 'Jane Doe' has been deleted")]
        [InlineData("Course", "7", null, EntityOperation.CREATE,
            "New Course (ID: 7) has been created")]
        [InlineData("Course", "7", "", EntityOperation.UPDATE,
            "Course (ID: 7) has been updated")]
        [InlineData("Department", "3", "Engineering", EntityOperation.DELETE,
            "Department 'Engineering' has been deleted")]
        public void GenerateMessage_ReturnsExpectedText(
            string entityType, string entityId, string displayName,
            EntityOperation operation, string expected)
        {
            var result = NotificationService.GenerateMessage(entityType, entityId, displayName, operation);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateMessage_UnknownOperation_ReturnsGenericText()
        {
            var result = NotificationService.GenerateMessage("Entity", "1", "Foo", (EntityOperation)99);

            Assert.Contains("operation: ", result);
        }

        // ── SendNotificationAsync ───────────────────────────────────────────────

        [Fact]
        public async Task SendNotificationAsync_WithDisplayName_SendsCorrectMessageBody()
        {
            // Arrange
            ServiceBusMessage capturedMessage = null;
            var senderMock = new Mock<ServiceBusSender>();
            senderMock
                .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            var receiverMock = new Mock<ServiceBusReceiver>();
            var svc = new NotificationService(senderMock.Object, receiverMock.Object, NullLogger<NotificationService>.Instance);

            // Act
            await svc.SendNotificationAsync("Student", "5", "Alice Smith", EntityOperation.CREATE, "admin");

            // Assert
            senderMock.Verify(
                s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.NotNull(capturedMessage);
            var body = capturedMessage.Body.ToString();
            var notification = JsonSerializer.Deserialize<Notification>(body);
            Assert.NotNull(notification);
            Assert.Equal("Student", notification.EntityType);
            Assert.Equal("5", notification.EntityId);
            Assert.Equal("CREATE", notification.Operation);
            Assert.Equal("admin", notification.CreatedBy);
            Assert.Equal("New Student 'Alice Smith' has been created", notification.Message);
            Assert.False(notification.IsRead);
        }

        [Fact]
        public async Task SendNotificationAsync_WithoutDisplayName_UsesIdFallback()
        {
            // Arrange
            ServiceBusMessage capturedMessage = null;
            var senderMock = new Mock<ServiceBusSender>();
            senderMock
                .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            var receiverMock = new Mock<ServiceBusReceiver>();
            var svc = new NotificationService(senderMock.Object, receiverMock.Object);

            // Act
            await svc.SendNotificationAsync("Course", "99", EntityOperation.DELETE);

            // Assert
            Assert.NotNull(capturedMessage);
            var notification = JsonSerializer.Deserialize<Notification>(capturedMessage.Body.ToString());
            Assert.Contains("ID: 99", notification.Message);
            Assert.Equal("System", notification.CreatedBy);
        }

        [Fact]
        public async Task SendNotificationAsync_WhenSenderThrows_DoesNotPropagateException()
        {
            // Arrange
            var senderMock = new Mock<ServiceBusSender>();
            senderMock
                .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusException("Simulated error", ServiceBusFailureReason.ServiceBusy));

            var receiverMock = new Mock<ServiceBusReceiver>();
            var svc = new NotificationService(senderMock.Object, receiverMock.Object, NullLogger<NotificationService>.Instance);

            // Act — should not throw
            await svc.SendNotificationAsync("Student", "1", EntityOperation.CREATE);
        }

        [Fact]
        public async Task SendNotificationAsync_SetsCreatedAtToUtcNow()
        {
            // Arrange
            ServiceBusMessage capturedMessage = null;
            var before = DateTime.UtcNow;
            var senderMock = new Mock<ServiceBusSender>();
            senderMock
                .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            var receiverMock = new Mock<ServiceBusReceiver>();
            var svc = new NotificationService(senderMock.Object, receiverMock.Object);

            // Act
            await svc.SendNotificationAsync("Instructor", "3", EntityOperation.UPDATE);
            var after = DateTime.UtcNow;

            // Assert
            var notification = JsonSerializer.Deserialize<Notification>(capturedMessage.Body.ToString());
            Assert.InRange(notification.CreatedAt, before.AddSeconds(-1), after.AddSeconds(1));
        }

        // ── ReceiveNotificationAsync ────────────────────────────────────────────

        [Fact]
        public async Task ReceiveNotificationAsync_WhenMessageAvailable_ReturnsDeserializedNotification()
        {
            // Arrange
            var original = new Notification
            {
                Id = 1,
                EntityType = "Student",
                EntityId = "10",
                Operation = "CREATE",
                Message = "New Student 'Bob' has been created",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                IsRead = false
            };
            var json = JsonSerializer.Serialize(original);
            var sbMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(json),
                messageId: Guid.NewGuid().ToString());

            var senderMock = new Mock<ServiceBusSender>();
            var receiverMock = new Mock<ServiceBusReceiver>();
            receiverMock
                .Setup(r => r.ReceiveMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sbMessage);
            receiverMock
                .Setup(r => r.CompleteMessageAsync(sbMessage, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var svc = new NotificationService(senderMock.Object, receiverMock.Object, NullLogger<NotificationService>.Instance);

            // Act
            var result = await svc.ReceiveNotificationAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Student", result.EntityType);
            Assert.Equal("10", result.EntityId);
            Assert.Equal("CREATE", result.Operation);

            receiverMock.Verify(
                r => r.CompleteMessageAsync(sbMessage, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ReceiveNotificationAsync_WhenQueueEmpty_ReturnsNull()
        {
            // Arrange
            var senderMock = new Mock<ServiceBusSender>();
            var receiverMock = new Mock<ServiceBusReceiver>();
            receiverMock
                .Setup(r => r.ReceiveMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceBusReceivedMessage)null);

            var svc = new NotificationService(senderMock.Object, receiverMock.Object);

            // Act
            var result = await svc.ReceiveNotificationAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ReceiveNotificationAsync_WhenReceiverThrows_ReturnsNull()
        {
            // Arrange
            var senderMock = new Mock<ServiceBusSender>();
            var receiverMock = new Mock<ServiceBusReceiver>();
            receiverMock
                .Setup(r => r.ReceiveMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusException("Connection lost", ServiceBusFailureReason.ServiceCommunicationProblem));

            var svc = new NotificationService(senderMock.Object, receiverMock.Object, NullLogger<NotificationService>.Instance);

            // Act — should not throw
            var result = await svc.ReceiveNotificationAsync();

            // Assert
            Assert.Null(result);
        }

        // ── MarkAsRead ──────────────────────────────────────────────────────────

        [Fact]
        public void MarkAsRead_IsNoOp_DoesNotThrow()
        {
            var senderMock = new Mock<ServiceBusSender>();
            var receiverMock = new Mock<ServiceBusReceiver>();
            var svc = new NotificationService(senderMock.Object, receiverMock.Object);

            // Should complete without any exception
            svc.MarkAsRead(42);
        }

        // ── Constructor guards ──────────────────────────────────────────────────

        [Fact]
        public void TestConstructor_NullSender_ThrowsArgumentNullException()
        {
            var receiverMock = new Mock<ServiceBusReceiver>();
            Assert.Throws<ArgumentNullException>(() =>
                new NotificationService(null, receiverMock.Object));
        }

        [Fact]
        public void TestConstructor_NullReceiver_ThrowsArgumentNullException()
        {
            var senderMock = new Mock<ServiceBusSender>();
            Assert.Throws<ArgumentNullException>(() =>
                new NotificationService(senderMock.Object, null));
        }

        // ── Multiple notifications increment IDs ────────────────────────────────

        [Fact]
        public async Task SendNotificationAsync_MultipleMessages_IdsAreUnique()
        {
            // Arrange
            var ids = new System.Collections.Generic.List<int>();
            var senderMock = new Mock<ServiceBusSender>();
            senderMock
                .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, _) =>
                {
                    var n = JsonSerializer.Deserialize<Notification>(msg.Body.ToString());
                    ids.Add(n.Id);
                })
                .Returns(Task.CompletedTask);

            var receiverMock = new Mock<ServiceBusReceiver>();
            var svc = new NotificationService(senderMock.Object, receiverMock.Object);

            // Act
            await svc.SendNotificationAsync("A", "1", EntityOperation.CREATE);
            await svc.SendNotificationAsync("B", "2", EntityOperation.UPDATE);
            await svc.SendNotificationAsync("C", "3", EntityOperation.DELETE);

            // Assert — IDs must be distinct (monotonically increasing)
            Assert.Equal(3, ids.Count);
            Assert.Equal(ids.Count, new System.Collections.Generic.HashSet<int>(ids).Count);
        }
    }
}
