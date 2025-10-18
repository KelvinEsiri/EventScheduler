# Offline Mode Quick Reference

## 🚨 Critical Rules

### DO ✅
- Use 500ms timeout for Blazor calls (not 2000ms)
- Convert dates to ISO 8601 using `.toISOString()`
- Check `navigator.onLine` before calling Blazor
- Fetch full event from IndexedDB before queueing sync
- Wrap all UI updates in try-catch for `JSDisconnectedException`
- Expose `isOnline` as getter property in connectivity manager
- Use `window.location.href` with timestamp for page reload

### DON'T ❌
- Call Blazor's default reconnection handlers (`origDown`, `origUp`)
- Use relative URLs in service worker sync
- Send date-only strings to API (use ISO 8601 datetime)
- Show "failed" errors when circuit disconnected but save succeeded
- Use `location.reload()` without query parameter
- Block on long timeouts (always race with timeout)

---

## 📁 File Versions

| File | Version | Purpose |
|------|---------|---------|
| `fullcalendar-interop.js` | v=15 | Calendar offline logic |
| `reconnection-handler.js` | v=14 | Circuit management |
| `connectivity-manager.js` | v=3 | Online/offline detection |
| `indexeddb-manager.js` | v=2 | Storage operations |

---

## 🔧 Common Code Patterns

### Pattern 1: Blazor Call with Timeout
```javascript
const blazorCall = this.invokeDotNet(elementId, 'Method', ...args);
const timeout = new Promise((_, reject) => 
    setTimeout(() => reject(new Error('timeout')), 500)
);

Promise.race([blazorCall, timeout])
    .then(/* success */)
    .catch(/* offline fallback */);
```

### Pattern 2: Check Connection Before Calling
```javascript
if (!this.checkBlazorConnection()) {
    // Go straight to offline mode
    this.handleOfflineOperation();
    return;
}
```

### Pattern 3: Save Offline with Full Data
```javascript
// 1. Fetch full event
const fullEvent = await window.indexedDBManager.getEvent(eventId);

// 2. Convert dates
const startDate = info.event.start.toISOString();
const endDate = info.event.end.toISOString();

// 3. Queue with ALL fields
await window.indexedDBManager.savePendingOperation({
    type: 'PUT',
    endpoint: `/api/events/${eventId}`,
    data: {
        title: fullEvent.title,              // ✅ Required!
        description: fullEvent.description,
        startDate: startDate,                // ✅ ISO 8601!
        endDate: endDate,                    // ✅ ISO 8601!
        location: fullEvent.location,
        isAllDay: info.event.allDay,
        color: fullEvent.color,
        categoryId: fullEvent.categoryId,
        status: fullEvent.status,
        eventType: fullEvent.eventType,
        isPublic: fullEvent.isPublic
    },
    token: localStorage.getItem('auth_token'),
    timestamp: new Date().toISOString()
});
```

### Pattern 4: C# Circuit Error Handling
```csharp
try
{
    var result = await OfflineEventService.UpdateEventAsync(id, request);
    ShowSuccess("Updated successfully!");
}
catch (JSDisconnectedException)
{
    Logger.LogWarning("Circuit disconnected but event should be saved offline");
    ShowSuccess("Saved offline - will sync when online");
}
catch (Exception ex)
{
    Logger.LogError(ex, "Error updating event");
    ShowError("Failed to update event.");
}
```

---

## 🐛 Quick Debugging

### Check Online State
```javascript
console.log({
    browser: navigator.onLine,
    manager: window.connectivityManager?.isOnline,
    blazor: window.fullCalendarInterop?.isBlazorConnected
});
```

### View Pending Operations
```javascript
window.indexedDBManager.getPendingOperations()
    .then(ops => console.table(ops));
```

### Clear Pending Operations
```javascript
await window.indexedDBManager.clearPendingOperations();
```

### Force Sync
```javascript
await window.connectivityManager.triggerSync();
```

### Check Date Format
```javascript
// Check pending operation
const ops = await window.indexedDBManager.getPendingOperations();
console.log('First operation date:', ops[0]?.data?.startDate);
// Should be: "2025-10-26T09:00:00.000Z" ✅
// NOT: "2025-10-26" ❌
```

---

## 🎯 Testing Checklist

- [ ] Drag event while online → Saves via API
- [ ] Drag event while offline → Saves to IndexedDB
- [ ] Click event while online → Opens modal
- [ ] Click event while offline → Shows cached alert
- [ ] Click date while offline → Shows info message
- [ ] Multiple drags offline → All queue properly
- [ ] Go back online → Page reloads, syncs all
- [ ] Check pending operations cleared after sync

---

## 🔍 Error Messages Reference

| Error | Meaning | Fix |
|-------|---------|-----|
| `Cannot send data if the connection is not in the 'Connected' State` | Circuit disconnected | Normal - offline fallback should handle |
| `400 Bad Request` | Wrong date format | Use `.toISOString()` |
| `405 Method Not Allowed` | Wrong endpoint | Use full URL in service worker |
| `JavaScript interop calls cannot be issued...statically rendered` | Prerendering | Normal - warning only |
| `Failed to initialize connectivity service` | Prerendering | Normal - retries after render |

---

## 📊 Performance Targets

| Metric | Target | Current |
|--------|--------|---------|
| Offline detection | < 500ms | 500ms ✅ |
| IndexedDB save | < 50ms | ~20ms ✅ |
| Sync per operation | < 2s | ~1.5s ✅ |
| Page reload | < 3s | ~2s ✅ |

---

## 🚀 Quick Start for New Developers

1. **Read**: `OFFLINE_MODE_COMPREHENSIVE_GUIDE.md` (Critical Components section)
2. **Test**: Run manual test #1 (Basic Offline Drag)
3. **Inspect**: Open DevTools → Application → IndexedDB during testing
4. **Debug**: Enable verbose logging (see guide)
5. **Modify**: Make small changes, test offline immediately
6. **Verify**: Check console for all expected log messages

---

## 📞 Emergency Contacts

**If offline mode breaks**:
1. Check console for errors
2. Check IndexedDB state
3. Clear pending operations if needed
4. Reload page with Ctrl+Shift+R
5. Check file versions match this guide

**Critical files to review**:
- `fullcalendar-interop.js` (Lines 9-40, 144-178, 334-383)
- `reconnection-handler.js` (Lines 52-96)
- `connectivity-manager.js` (Lines 32-58, 164-168)

---

**Last Updated**: October 18, 2025  
**Quick Reference Version**: 1.0
