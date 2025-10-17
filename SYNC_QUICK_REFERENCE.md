# Synchronization Quick Reference Card

## Quick Problem → Solution Lookup

### Problem: Events appearing twice when creating online
**Solution:** ID-based tracking with `optimisticallyAddedEventIds`  
**File:** `CalendarView.razor.cs`  
**How it works:** Track event ID immediately after creation, skip SignalR broadcast for tracked IDs

### Problem: Offline events duplicating after sync
**Solution:** Temp-to-real ID mapping with `tempIdToRealIdMap`  
**File:** `OfflineSyncService.cs`, `CalendarView.razor.cs`  
**How it works:** Map temporary negative IDs to real server IDs, auto-replace in UI

### Problem: Update/Delete on offline events failing
**Solution:** Smart operation merging  
**File:** `OfflineSyncService.cs`  
**How it works:** Merge updates into pending creates, cancel creates for deletes

### Problem: Cache and server out of sync
**Solution:** Auto-cleanup after sync + reload  
**File:** `OfflineSyncService.cs`, `CalendarView.razor.cs`  
**How it works:** Remove temp events from cache, reload all events after sync

---

## Key Data Structures

```csharp
// Tracks events added optimistically (online creates)
private readonly HashSet<int> optimisticallyAddedEventIds = new();

// Maps temporary IDs to real server IDs
private readonly Dictionary<int, int> tempIdToRealIdMap = new();

// Tracks pending local changes (updates/deletes)
private readonly HashSet<int> pendingLocalChanges = new();
```

---

## Event ID Conventions

| Type | Range | Example | Usage |
|------|-------|---------|-------|
| Real Events | Positive integers | 1, 2, 123 | Events from server |
| Temporary Events | Negative integers | -999, -123456 | Offline-created events |

---

## Key Methods Reference

### Creating Event
```csharp
// Online
var event = await ApiService.CreateEventAsync(request);
optimisticallyAddedEventIds.Add(event.Id);  // Track to prevent duplicate

// Offline
var event = await OfflineSyncService.CreateEventOfflineAsync(request);
// TempId stored in pending operation automatically
```

### SignalR Handler
```csharp
hubConnection.On<EventResponse>("EventCreated", async (eventData) => {
    // Check optimistic tracking
    if (optimisticallyAddedEventIds.Remove(eventData.Id)) {
        return; // Skip - our own event
    }
    
    // Check temp ID mapping
    var tempId = tempIdToRealIdMap.FirstOrDefault(kvp => kvp.Value == eventData.Id).Key;
    if (tempId != 0) {
        // Replace temp event with real event
        // Remove from map
        return;
    }
    
    // Check if already exists
    if (!events.Any(e => e.Id == eventData.Id)) {
        events.Add(eventData); // From another user/tab
    }
});
```

### Updating Event
```csharp
// Check if temporary event
if (eventId < 0) {
    // Find pending create and merge
    var createOp = pendingOps.FirstOrDefault(op => op.TempId == eventId);
    // Merge update into create request
} else {
    // Queue normal update operation
}
```

### Deleting Event
```csharp
// Check if temporary event
if (eventId < 0) {
    // Remove pending create operation
    // No server sync needed
} else {
    // Queue normal delete operation
}
```

---

## Sync Flow Quick Reference

```
ONLINE CREATE:
Create → Track ID → Show → SignalR → Check Track → Skip

OFFLINE CREATE:
Create → Temp ID → Cache → Queue → ONLINE → Sync → Map IDs → Replace → Cleanup

UPDATE TEMP EVENT:
Update → Check Temp → Find Create → Merge → Save

DELETE TEMP EVENT:
Delete → Check Temp → Remove Create → Done
```

---

## Debugging Checklist

### Events appearing twice?
- [ ] Check console for `optimisticallyAddedEventIds` tracking
- [ ] Verify SignalR handler is checking the HashSet
- [ ] Look for "Skipping SignalR EventCreated" message

### Temp events not syncing?
- [ ] Check IndexedDB → pendingOperations for queued operations
- [ ] Verify TempId field is present in operations
- [ ] Look for "Mapped temporary ID" messages in console
- [ ] Check if OnTempIdMapped event is firing

### Cache issues?
- [ ] Check IndexedDB → events store for orphaned temp events
- [ ] Verify cleanup happens after sync ("Removed temporary event" log)
- [ ] Ensure events reload after sync completes

### Operations not processing?
- [ ] Check network status (should be online)
- [ ] Verify SemaphoreSlim isn't blocking ("Sync already in progress")
- [ ] Check for operation validation errors in console

---

## Console Log Patterns

### Normal Operation (Online Create)
```
Event 123 added optimistically
SignalR: EventCreated (ID: 123)
Skipping SignalR EventCreated - already added optimistically (Event 123)
```

### Normal Operation (Offline Create + Sync)
```
Event -999 created offline
Network status changed to Online
Found 1 pending operations - triggering automatic sync
Syncing create operation: Meeting
Mapped temporary ID -999 to real ID 123
Replacing temporary event -999 with real event 123
Removed temporary event -999 from cache after sync
Synchronization completed: 1 operations synced successfully
```

### Normal Operation (Update Temp Event)
```
Updating temporary event -999 - will merge with pending create operation
Merged update into pending create operation for temp event -999
```

### Normal Operation (Delete Temp Event)
```
Deleting temporary event -999 - will remove pending create operation
Removed pending create operation for temp event -999
```

---

## Testing Quick Commands

### Test Duplicate Prevention (Online)
1. Create event
2. Check console for "added optimistically"
3. Wait for SignalR
4. Check for "Skipping SignalR"
5. Verify only one event in UI

### Test Offline Sync
1. DevTools → Network → Offline
2. Create event (note temp ID)
3. Network → No throttling
4. Wait for sync
5. Check console for ID mapping
6. Verify event has real ID

### Test Multi-Client
1. Open 2 tabs
2. Tab 1: Create event
3. Tab 2: Should see notification
4. Both tabs should have event once

---

## File Reference

| File | Purpose |
|------|---------|
| `CalendarView.razor.cs` | Main UI logic, duplicate prevention |
| `OfflineSyncService.cs` | Sync orchestration, temp ID mapping |
| `OfflineStorageService.cs` | IndexedDB persistence |
| `offline-storage.js` | IndexedDB operations |
| `NetworkStatusService.cs` | Online/offline detection |

---

## Common Patterns

### Check if Event is Temporary
```csharp
if (eventId < 0) {
    // Temporary event (offline-created)
} else {
    // Real event (from server)
}
```

### Track Optimistic Addition
```csharp
optimisticallyAddedEventIds.Add(event.Id);
```

### Map Temp to Real ID
```csharp
tempIdToRealIdMap[tempId] = realId;
```

### Track Pending Change
```csharp
pendingLocalChanges.Add(eventId);
```

### Check and Remove from Tracking
```csharp
if (optimisticallyAddedEventIds.Remove(eventId)) {
    // This was our own event
}
```

---

## Key Events

| Event | When Fired | Purpose |
|-------|------------|---------|
| `OnTempIdMapped` | After sync creates event | Notify UI to replace temp with real |
| `OnPendingOperationsCountChanged` | After queue changes | Update UI counter |
| `OnSyncStatusChanged` | During sync | Show sync progress |
| `OnStatusChanged` (Network) | Network change | Trigger auto-sync |

---

## Performance Tips

1. **Operation Merging**: Updates/deletes on temp events don't create extra API calls
2. **Batch Sync**: Multiple operations sync in single batch
3. **Optimistic UI**: Instant feedback without waiting for server
4. **Smart Caching**: Only reload when necessary

---

## Security Notes

✅ All changes passed CodeQL security scan  
✅ No vulnerabilities introduced  
✅ Proper input validation maintained  
✅ JWT authentication unchanged  

---

## Migration Notes

**No breaking changes** - all improvements are transparent:
- Existing events continue to work
- IndexedDB schema auto-upgrades
- No user action required
- No API changes

---

## Support & Documentation

- **Full Guide:** `SYNC_FIXES_SUMMARY.md`
- **Visual Flows:** `SYNC_FLOW_DIAGRAMS.md`
- **Testing:** `SYNC_TESTING_CHECKLIST.md`
- **Technical Details:** `OFFLINE_IMPROVEMENTS_SUMMARY.md`

---

**Quick Start Testing:** Use `SYNC_TESTING_CHECKLIST.md` Test Suite 1 for basic verification.

**Version:** 2.0 (Advanced Synchronization)  
**Last Updated:** 2025-10-17
