# Modernization Summary: 002-transform-msmq-to-servicebus

## Task
Migrate the MSMQ-based `NotificationService` from `System.Messaging` (in-memory placeholder) to Azure Service Bus with Managed Identity authentication.

## Changes Made

### New Files
| File | Description |
|------|-------------|
| `Tests/ContosoUniversity.Tests.csproj` | New xUnit + Moq test project targeting net10.0 |
| `Tests/Services/NotificationServiceTests.cs` | 18 unit tests covering send, receive, error handling, and message generation |

### Modified Files

#### `Services/NotificationService.cs` — Complete Rewrite
- **Removed**: `ConcurrentQueue<Notification>` in-memory implementation, `System.Collections.Concurrent` dependency
- **Added**: `INotificationService` interface for testability
- **Added**: `ServiceBusClient`, `ServiceBusSender`, `ServiceBusReceiver` — all initialized with `DefaultAzureCredential` (Managed Identity, no connection strings)
- **Added**: `SendNotificationAsync` — serializes `Notification` to JSON, sends as `ServiceBusMessage`
- **Added**: `ReceiveNotificationAsync` — receives, deserializes, and completes messages; uses 200ms timeout for responsive polling
- **Added**: Internal test constructor accepting mock `ServiceBusSender`/`ServiceBusReceiver`
- **Changed**: `GenerateMessage` is now `internal static` for test access
- **Changed**: Proper `IDisposable` implementation disposes sender, receiver, and client
- **Changed**: `DateTime.Now` → `DateTime.UtcNow` (cloud best practice)

#### `Controllers/BaseController.cs`
- Renamed `SendEntityNotification` → `SendEntityNotificationAsync` returning `Task`
- Uses `await notificationService.SendNotificationAsync(...)` with `ConfigureAwait(false)`

#### `Controllers/NotificationsController.cs`
- `GetNotifications()` → `async Task<IActionResult> GetNotifications()`
- Polling loop uses `await notificationService.ReceiveNotificationAsync()`

#### `Controllers/StudentsController.cs`
- `Create`, `Edit`, `DeleteConfirmed` → `async Task<IActionResult>` with `await SendEntityNotificationAsync`
- Added `using System.Threading.Tasks`

#### `Controllers/CoursesController.cs`
- `Create`, `Edit`, `DeleteConfirmed` → `async Task<IActionResult>` with `await SendEntityNotificationAsync`
- Added `using System.Threading.Tasks`

#### `Controllers/DepartmentsController.cs`
- `Create`, `Edit`, `DeleteConfirmed` → `async Task<IActionResult>` with `await SendEntityNotificationAsync`
- Added `using System.Threading.Tasks`

#### `Controllers/InstructorsController.cs`
- `Create`, `DeleteConfirmed` → `async Task<IActionResult>` with `await SendEntityNotificationAsync`
- `Edit` (already async) updated to `await SendEntityNotificationAsync`

#### `appsettings.json`
```json
"AzureServiceBus": {
  "FullyQualifiedNamespace": "YOUR_SERVICEBUS_NAMESPACE.servicebus.windows.net",
  "QueueName": "notifications"
}
```

#### `ContosoUniversity.csproj`
- Added `Azure.Messaging.ServiceBus` 7.19.0
- Added `Azure.Identity` 1.14.2 (minimum required by transitive dependencies)
- Added `<Compile Remove="Tests\**\*" />` exclusion block

#### `Properties/AssemblyInfo.cs`
- Added `[assembly: InternalsVisibleTo("ContosoUniversity.Tests")]`

#### `Program.cs`
- Updated singleton registration comment to reflect Azure Service Bus

## Authentication Pattern
```csharp
var credential = new DefaultAzureCredential();
var client = new ServiceBusClient(fullyQualifiedNamespace, credential);
```
No connection strings, API keys, or secrets are used anywhere.

## Test Results
- **Build**: ✅ 0 errors, 0 warnings
- **Unit Tests**: ✅ 18/18 passed
  - Message generation for CREATE / UPDATE / DELETE with and without display name
  - Correct `ServiceBusMessage` body serialization (JSON)
  - Sender mock verification (called exactly once)
  - Receiver mock deserialization and `CompleteMessageAsync` verification
  - Error swallowing when sender / receiver throw
  - Null returns when queue is empty or errors occur
  - Unique incrementing IDs across multiple sends
  - Constructor guard for null sender / receiver
  - `MarkAsRead` is a no-op (no exceptions)

## Old Technology References Removed
| Old Reference | Replacement |
|---------------|-------------|
| `System.Collections.Concurrent.ConcurrentQueue<T>` | `ServiceBusReceiver.ReceiveMessageAsync` |
| `_queue.Enqueue(notification)` | `ServiceBusSender.SendMessageAsync` |
| `_queue.TryDequeue` | `ServiceBusReceiver.ReceiveMessageAsync` |
| In-memory singleton state | Azure Service Bus cloud queue |
