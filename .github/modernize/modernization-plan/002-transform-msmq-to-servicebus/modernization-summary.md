# Task 002 — Migrate MSMQ to Azure Service Bus

## Summary

Replaced the in-memory / `System.Threading.Channels` `NotificationService` (legacy MSMQ stand-in) with a full Azure Service Bus implementation using Managed Identity (`DefaultAzureCredential`).

---

## Changes Made

### `Services/NotificationService.cs` — Full rewrite
- **Before**: Bounded `System.Threading.Channels.Channel<Notification>` (in-memory, single-process only).
- **After**: Azure Service Bus SDK (`Azure.Messaging.ServiceBus`) with `DefaultAzureCredential`.
- Implements `IHostedService` so the `ServiceBusProcessor` lifecycle (start/stop) is managed by the ASP.NET Core host.
- Implements both `IAsyncDisposable` and `IDisposable` for proper resource cleanup.
- `SendNotification` fire-and-forgets a `ServiceBusMessage` (JSON body) to the configured queue.
- `ReceiveNotification` drains a `ConcurrentQueue<Notification>` that the background `ServiceBusProcessor` populates.
- `GetAllNotifications` / `MarkAsRead` operate on an in-memory list (same contract as before).
- All logging upgraded to `ILogger<NotificationService>`.
- Timestamps changed to `DateTime.UtcNow` (cloud best-practice).

### `Program.cs`
- Changed registration from `AddSingleton<NotificationService>()` only to:
  ```csharp
  builder.Services.AddSingleton<NotificationService>();
  builder.Services.AddHostedService(sp => sp.GetRequiredService<NotificationService>());
  ```
  This wires up `StartAsync`/`StopAsync` without creating a second instance.

### `appsettings.json` / `appsettings.Development.json`
- Removed `AppSettings:NotificationQueuePath` (MSMQ path).
- Added `AzureServiceBus` section:
  ```json
  "AzureServiceBus": {
    "FullyQualifiedNamespace": "${SERVICE_BUS_NAMESPACE}.servicebus.windows.net",
    "QueueName": "contoso-university-notifications"
  }
  ```

### `ContosoUniversity.csproj`
- Added NuGet packages: `Azure.Messaging.ServiceBus` (7.19.0), `Azure.Identity` (1.14.0).
- Added `InternalsVisibleTo` attribute for the test project.
- Excluded `ContosoUniversity.Tests/**` and `modernize/**` from SDK compile/content globs.

### `ContosoUniversity.Tests/` — New test project
- New `xUnit` test project `ContosoUniversity.Tests.csproj` (net10.0).
- `Services/NotificationServiceTests.cs` — 21 unit tests covering:
  - `SendNotification` (adds to list, sends to Service Bus, JSON body, display name, operation types, default user, ID uniqueness)
  - `ReceiveNotification` (empty queue returns null, message delivered by processor, queue drain, multiple messages)
  - `GetAllNotifications` (empty initially, newest-first ordering)
  - `MarkAsRead` (sets flag, unknown ID no-throw, only marks specified notification)
  - `StartAsync` / `StopAsync` (processor lifecycle)
- Test doubles: `FakeServiceBusSender` (records sent messages) and `FakeServiceBusProcessor` (tracks lifecycle).
- Uses `ServiceBusModelFactory` for constructing test `ServiceBusReceivedMessage` instances.

---

## Old Technology Removed

| Technology | Status |
|---|---|
| `System.Threading.Channels` | ✅ Removed from `NotificationService.cs` |
| `System.Messaging` / MSMQ references | ✅ None present in codebase |
| `AppSettings:NotificationQueuePath` | ✅ Removed from both `appsettings` files |

---

## Exit Criteria Status

| Criterion | Status |
|---|---|
| Build passes (`passBuild`) | ✅ 0 errors, 0 warnings |
| Unit tests generated (`generateNewUnitTests`) | ✅ 21 new unit tests |
| Unit tests pass (`passUnitTests`) | ✅ 21/21 passed |
| Consistency check | ✅ No Critical or Major issues |
| Old technology fully removed | ✅ No MSMQ/Channel references remain |
