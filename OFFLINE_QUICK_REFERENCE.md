# Offline Support - Quick Reference

## 🚀 Quick Start

### 1. Use OfflineEventService
```csharp
@inject OfflineEventService EventService

// All operations work online or offline
var events = await EventService.GetEventsAsync();
var newEvent = await EventService.CreateEventAsync(request);
var updated = await EventService.UpdateEventAsync(id, request);
await EventService.DeleteEventAsync(id);
```

### 2. Monitor Connectivity
```csharp
@inject ConnectivityService ConnectivityService

protected override async Task OnInitializedAsync()
{
    await ConnectivityService.InitializeAsync();
    isOnline = ConnectivityService.IsOnline;
    ConnectivityService.ConnectivityChanged += OnConnectivityChanged;
}
```

### 3. Handle Sync Events
```csharp
@inject SyncService SyncService

protected override void OnInitialized()
{
    SyncService.SyncCompleted += async (s, result) => {
        if (result.Success) await RefreshData();
    };
}
```

## 📋 Common Patterns

### Check Online Status
```csharp
if (ConnectivityService.IsOnline)
{
    // Online-only operation
}
else
{
    // Show offline message
}
```

### Manual Sync
```csharp
private async Task TriggerSync()
{
    var result = await SyncService.SyncAsync();
    if (result.Success)
    {
        message = $"Synced {result.SyncedEvents} events";
    }
}
```

### Get Pending Count
```csharp
var pendingCount = await SyncService.GetPendingOperationsCountAsync();
```

### Clear Cache (on Logout)
```csharp
await LocalStorage.ClearAllDataAsync();
```

## 🎯 Services Overview

| Service | Purpose | Key Methods |
|---------|---------|-------------|
| **OfflineEventService** | Offline-first CRUD | GetEventsAsync, CreateEventAsync, UpdateEventAsync, DeleteEventAsync |
| **ConnectivityService** | Network monitoring | InitializeAsync, IsOnline, ConnectivityChanged event |
| **SyncService** | Synchronization | SyncAsync, GetPendingOperationsCountAsync, SyncStarted/Completed events |
| **LocalStorageService** | IndexedDB wrapper | SaveEventAsync, GetAllEventsAsync, SavePendingOperationAsync |

## 🔧 Configuration

### Already Configured in Program.cs
```csharp
builder.Services.AddScoped<ConnectivityService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<OfflineEventService>();
```

### Already Added to Layout
```razor
<!-- MainLayout.razor -->
<OfflineIndicator />
```

## 🧪 Testing Offline

### Browser DevTools
1. F12 → Network tab
2. Select "Offline" from dropdown
3. Test CRUD operations
4. Select "Online" → watch auto-sync

### Service Worker
1. F12 → Application tab
2. Service Workers → Check "Offline"
3. Test functionality
4. Uncheck → reconnect

## 📊 UI Indicators

### Events with Temporary IDs (created offline)
```razor
@if (evt.Id < 0)
{
    <span class="badge bg-warning">Pending Sync</span>
}
```

### Connectivity Status
```razor
<span class="@(isOnline ? "text-success" : "text-warning")">
    <i class="bi @(isOnline ? "bi-wifi" : "bi-wifi-off")"></i>
    @(isOnline ? "Online" : "Offline")
</span>
```

### Pending Operations
```razor
@if (pendingCount > 0)
{
    <span class="badge bg-info">@pendingCount pending</span>
}
```

## ⚠️ Important Notes

1. **Must load page online first** (Blazor Server requirement)
2. **Temporary IDs are negative** - handle in UI comparisons
3. **Clear cache on logout** - prevent data leaks
4. **Check connectivity** for online-only operations
5. **Subscribe to events** for real-time updates

## 🎨 CSS Classes Available

- `.offline-indicator` - Container for offline UI
- `.offline-banner` - Yellow offline mode banner
- `.syncing-banner` - Blue syncing indicator
- `.synced-banner` - Green success message

## 📚 Documentation

- **Architecture**: [OFFLINE_SUPPORT_ARCHITECTURE.md](OFFLINE_SUPPORT_ARCHITECTURE.md)
- **Developer Guide**: [OFFLINE_SUPPORT_GUIDE.md](OFFLINE_SUPPORT_GUIDE.md)
- **Summary**: [OFFLINE_SUPPORT_SUMMARY.md](OFFLINE_SUPPORT_SUMMARY.md)
- **Technical Notes**: [OFFLINE_IMPLEMENTATION_NOTES.md](OFFLINE_IMPLEMENTATION_NOTES.md)

## 🐛 Troubleshooting

### Events not syncing?
```csharp
// Check pending operations
var pending = await LocalStorage.GetPendingOperationsAsync();
Console.WriteLine($"Pending: {pending.Count}");

// Manual sync
await SyncService.SyncAsync();
```

### Service Worker not working?
1. Check HTTPS (or localhost)
2. Clear browser cache
3. Check console for errors
4. Verify service-worker.js path

### IndexedDB errors?
```csharp
// Clear and reinitialize
await LocalStorage.ClearAllDataAsync();
await LocalStorage.InitializeAsync();
```

## ✨ Best Practices

1. ✅ Always use OfflineEventService (not ApiService)
2. ✅ Subscribe to connectivity changes
3. ✅ Handle sync events for UI updates
4. ✅ Show pending operations count
5. ✅ Clear cache on logout
6. ✅ Test offline scenarios
7. ✅ Dispose event subscriptions

## 🔄 Migration Checklist

- [ ] Replace `@inject ApiService` with `@inject OfflineEventService`
- [ ] Replace `GetAllEventsAsync()` with `GetEventsAsync()`
- [ ] Replace `GetEventByIdAsync(id)` with `GetEventAsync(id)`
- [ ] Add connectivity monitoring if needed
- [ ] Add sync event handlers if needed
- [ ] Test offline scenarios
- [ ] Update documentation

## 📞 Support

For issues or questions:
1. Check documentation files
2. Review code examples
3. Test in browser DevTools
4. Check console logs
5. Clear cache and retry

---

**EventScheduler Offline Support** - Making your calendar work anywhere, anytime! 🚀
