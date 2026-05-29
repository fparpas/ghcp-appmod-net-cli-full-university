# Task 02.02 - Notification Service: Progress Details

## Completed: Replaced System.Messaging (MSMQ) with Channel<Notification>

### Problem
`NotificationService` used `System.Messaging.MessageQueue` (MSMQ), which has no .NET Core equivalent.

### Solution
Replaced with `System.Threading.Channels.Channel<Notification>` — a .NET Core-native bounded in-memory channel.

### Files Modified
- **`Services/NotificationService.cs`** — Complete rewrite:
  - Removed: `System.Messaging`, `System.Configuration`, `Newtonsoft.Json`
  - Added: `System.Threading.Channels`, in-memory list for GetAllNotifications()
  - New methods: `GetAllNotifications()` returns all notifications newest-first
  - `MarkAsRead()` now works (updates in-memory list)
  - Channel capacity: 100, FullMode: DropOldest (same semantics as MSMQ bounded queue)
  - `_nextId` counter assigns sequential IDs (simple in-memory strategy for demo app)

### API Mapping Applied
| Old (MSMQ) | New (Channel) |
|---|---|
| `MessageQueue.Create(path)` | `Channel.CreateBounded<Notification>(100)` |
| `_queue.Send(message)` | `_channel.Writer.TryWrite(notification)` |
| `_queue.Receive(timeout)` | `_channel.Reader.TryRead(out notification)` |
| `MessageQueueException IOTimeout` | `TryRead` returns false (no exception needed) |
| `new Message(json)` | Direct `Notification` object (no serialization needed) |

### Build Status After This Task
- `System.Messaging` errors: **0** (was 8)
- `System.Web.Mvc` errors: **~126** (expected — controller migration tasks 02.03-02.09)
- No regressions introduced
