# 02.02-notification-service: Replace System.Messaging (MSMQ) with an in-memory Channel<T> implementation

## Objective
Replace System.Messaging / MSMQ usage in NotificationService with a .NET Core compatible in-memory implementation using System.Threading.Channels. System.Messaging has no .NET Core equivalent and must be replaced.

## Scope
- Services/NotificationService.cs: replace MessageQueue, Message, MessageQueueTransaction with Channel<Notification>
- Remove System.Messaging assembly reference from csproj
- Update BaseController.cs to work with the new NotificationService interface
- Update NotificationsController.cs if needed
- Services/LoggingService.cs: review and fix excluded file

## API Mapping
- MessageQueue.Create → Channel<Notification>.CreateBounded(100)
- _queue.Send(message) → channel.Writer.TryWrite(notification)
- _queue.Receive() → channel.Reader.TryRead(out notification)
- MessageQueueException → Channel-based timeout/empty pattern

## Done when
NotificationService compiles with 0 errors, no System.Messaging references remain, Channel<Notification> implementation provides equivalent Send/Receive/MarkAsRead contract.
