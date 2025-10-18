# Offline Mode Comprehensive Guide

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Critical Components](#critical-components)
4. [Data Flow](#data-flow)
5. [Testing Guidelines](#testing-guidelines)
6. [Troubleshooting](#troubleshooting)
7. [Known Limitations](#known-limitations)
8. [Future Enhancements](#future-enhancements)

---

## Overview

The EventScheduler application supports **full offline functionality** for viewing and modifying calendar events. When offline, all changes are saved locally in IndexedDB and automatically synchronized when the connection is restored.

### Key Features
- ✅ **Offline-first data loading** - Always loads from cache first
- ✅ **Real-time offline detection** - 500ms response time
- ✅ **Drag & drop offline** - Move events without connection
- ✅ **Automatic sync** - Queues operations and syncs when online
- ✅ **Conflict resolution** - Last-write-wins strategy
- ✅ **Progressive Web App** - Installable with service worker

---

## Architecture

### High-Level Overview
```
┌─────────────────────────────────────────────────────────────┐
│                      User Interface                          │
│  (Blazor Components - CalendarView.razor)                   │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              Offline-First Services (C#)                     │
│  ┌──────────────────┐  ┌──────────────────┐                │
│  │ OfflineEventService│  │ConnectivityService│               │
│  └──────────────────┘  └──────────────────┘                │
│  ┌──────────────────┐  ┌──────────────────┐                │
│  │LocalStorageService│  │   SyncService    │                │
│  └──────────────────┘  └──────────────────┘                │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│           JavaScript Managers (Browser-side)                 │
│  ┌──────────────────┐  ┌──────────────────┐                │
│  │  IndexedDB Manager│  │Connectivity Mgr  │                │
│  └──────────────────┘  └──────────────────┘                │
│  ┌──────────────────┐  ┌──────────────────┐                │
│  │FullCalendar Interop│ │Service Worker    │                │
│  └──────────────────┘  └──────────────────┘                │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                      IndexedDB                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │events store  │  │pendingOps    │  │syncMetadata  │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

### Technology Stack
- **Backend**: ASP.NET Core 8.0 (Blazor Server)
- **Real-time**: SignalR over WebSocket
- **Client Storage**: IndexedDB API
- **Offline Caching**: Service Worker (Cache API)
- **Calendar UI**: FullCalendar v6.1.10
- **Sync Strategy**: Background Sync API

---

## Critical Components

### ⚠️ HIGH ATTENTION AREAS

#### 1. **Blazor Circuit Management** 
**Location**: `wwwroot/js/reconnection-handler.js` (v=14)

**CRITICAL**: This component manages Blazor SignalR circuit disconnections and prevents UI freezing.

**Key Points**:
- **Line 52-86**: Connection down handler - MUST NOT call `origDown.call(handler)`
- **Line 88-96**: Connection up handler - MUST reload page with timestamp query parameter
- **Line 75**: Sets `fullCalendarInterop.isBlazorConnected = false` for offline mode

**⚠️ WARNING**: 
- DO NOT call Blazor's default reconnection handlers (`origDown`, `origUp`)
- This will cause the page to freeze and show blocking reconnection UI
- Always use custom handlers that reload the page instead

```javascript
// ❌ WRONG - Will freeze UI
handler.onConnectionDown = function() {
    modal.className = 'components-reconnect-hide';
    if (origDown) origDown.call(handler); // ❌ BAD!
};

// ✅ CORRECT - Keeps app working
handler.onConnectionDown = function() {
    modal.className = 'components-reconnect-hide';
    window.fullCalendarInterop.isBlazorConnected = false;
    // DON'T call origDown - handle everything ourselves
};
```

---

#### 2. **FullCalendar Offline Fallback**
**Location**: `wwwroot/js/fullcalendar-interop.js` (v=15)

**CRITICAL**: This component handles all calendar interactions with offline detection.

**Key Points**:
- **Line 9-40**: `checkBlazorConnection()` - Multi-layer connection detection
- **Line 144-178**: `eventDrop` handler - 500ms timeout for offline detection
- **Line 334-383**: `handleOfflineEventDrop()` - Pure JavaScript offline save
- **Line 95-132**: `eventClick` handler - Shows cached data when offline

**⚠️ WARNING**:
- MUST check connection BEFORE calling Blazor methods
- MUST use 500ms timeout (not 2000ms) for fast offline detection
- MUST fetch full event from IndexedDB before queueing sync (includes all required fields)
- MUST convert dates to ISO 8601 format using `.toISOString()`

**Critical Timeout Pattern**:
```javascript
// ✅ CORRECT - Fast offline detection
const timeoutPromise = new Promise((_, reject) => 
    setTimeout(() => reject(new Error('timeout')), 500) // 500ms!
);

Promise.race([blazorCall, timeoutPromise])
    .then(/* success */)
    .catch(/* offline fallback */);

// ❌ WRONG - Too slow
setTimeout(() => reject(new Error('timeout')), 2000) // Too long!
```

**Critical Date Format**:
```javascript
// ✅ CORRECT - ISO 8601 datetime
const startDate = info.event.start.toISOString(); 
// Result: "2025-10-26T09:00:00.000Z"

// ❌ WRONG - Date only string
const startDate = info.event.startStr; 
// Result: "2025-10-26" - API will reject!
```

---

#### 3. **Connectivity Detection**
**Location**: `wwwroot/js/connectivity-manager.js` (v=3)

**CRITICAL**: This component provides the source of truth for online/offline status.

**Key Points**:
- **Line 5**: `isOnline` initialized from `navigator.onLine`
- **Line 45-58**: `handleOffline()` - Updates FullCalendar state immediately
- **Line 32-44**: `handleOnline()` - Triggers sync and restores online mode
- **Line 164-168**: Exposes `isOnline` as public property (getter)

**⚠️ WARNING**:
- MUST expose `isOnline` as a getter property (not just via `getStatus()`)
- MUST update `fullCalendarInterop.isBlazorConnected` when state changes
- Browser's `navigator.onLine` is the fastest detection method

```javascript
// ✅ CORRECT - Property getter exposed
return {
    init,
    getStatus,
    get isOnline() {
        return isOnline;
    }
};

// ❌ WRONG - Property not accessible
return {
    init,
    getStatus // Only method, no property access
};
```

---

#### 4. **IndexedDB Date Format**
**Location**: `wwwroot/js/fullcalendar-interop.js` (Lines 326-340)

**CRITICAL**: API requires ISO 8601 datetime format, not date-only strings.

**The Issue**:
- FullCalendar provides `info.event.startStr` as "2025-10-26" (date only)
- API expects "2025-10-26T09:00:00.000Z" (datetime with timezone)
- Sending date-only strings causes 400 Bad Request errors

**Solution**:
```javascript
// ✅ CORRECT - Use .toISOString() on Date objects
const startDate = info.event.start ? info.event.start.toISOString() : info.event.startStr;
const endDate = info.event.end ? info.event.end.toISOString() : info.event.endStr;

await window.indexedDBManager.savePendingOperation({
    type: 'PUT',
    endpoint: `/api/events/${eventId}`,
    data: {
        title: fullEvent.title,
        startDate: startDate,  // ISO 8601: "2025-10-26T09:00:00.000Z"
        endDate: endDate,      // ISO 8601: "2025-10-26T10:00:00.000Z"
        // ... other fields
    }
});
```

**⚠️ WARNING**: 
- Old pending operations with wrong date format will cause 400 errors during sync
- If you encounter 400 errors, clear pending operations: `indexedDBManager.clearPendingOperations()`

---

#### 5. **Service Worker Sync**
**Location**: `wwwroot/service-worker.js` (CACHE_VERSION='v2')

**CRITICAL**: Handles background sync of pending operations.

**Key Points**:
- **Line 193-224**: `processSyncOperation()` - Constructs full API URLs
- **Line 195**: Must construct full URL: `http://localhost:5006/api/events/...`
- **Line 210-218**: Handles sync success/failure

**⚠️ WARNING**:
- Service worker runs in separate context (no access to Blazor)
- MUST construct full API URLs (relative URLs fail)
- Failed operations are NOT removed from queue (will retry)

```javascript
// ✅ CORRECT - Full URL construction
const apiBaseUrl = 'http://localhost:5006';
const fullUrl = endpoint.startsWith('http') 
    ? endpoint 
    : `${apiBaseUrl}${endpoint}`;

// ❌ WRONG - Relative URL (405 errors)
const response = await fetch(operation.endpoint); // Fails!
```

---

#### 6. **C# Error Handling**
**Location**: `Components/Pages/CalendarView.razor.cs`

**CRITICAL**: Must handle circuit disconnections gracefully.

**Key Points**:
- **Line 969-979**: SaveEvent with JSDisconnectedException handling
- **Line 270-310**: ShowSuccess/ShowError with error wrapping
- **Line 817**: UpdateEventAsync call wrapped in try-catch

**⚠️ WARNING**:
- MUST catch `JSDisconnectedException` separately
- MUST NOT show "Failed to update" when circuit is disconnected but save succeeded
- UI update methods MUST be wrapped in try-catch

```csharp
// ✅ CORRECT - Specific exception handling
try
{
    var updatedEvent = await OfflineEventService.UpdateEventAsync(editEventId, updateRequest);
    ShowSuccess("Event updated successfully!");
}
catch (JSDisconnectedException)
{
    // Circuit disconnected but offline service saved it
    Logger.LogWarning("Circuit disconnected during update, but event should be saved offline");
    ShowSuccess("Event saved offline - will sync when online");
}
catch (Exception ex)
{
    Logger.LogError(ex, "Error saving event");
    ShowError("Failed to update event.");
}

// ❌ WRONG - Generic catch shows wrong message
catch (Exception ex)
{
    ShowError("Failed to update event."); // User sees error even though save worked!
}
```

---

## Data Flow

### Offline Event Update Flow

```
User drags event
        │
        ▼
[FullCalendar] eventDrop handler fires
        │
        ├─── Check: isBlazorConnected?
        │    │
        │    ├─── YES → Try Blazor call (500ms timeout)
        │    │         │
        │    │         ├─── SUCCESS → Update via API ✅
        │    │         │
        │    │         └─── TIMEOUT/ERROR → Fall through to offline ⬇
        │    │
        │    └─── NO → Skip to offline mode ⬇
        │
        ▼
[Offline Mode] handleOfflineEventDrop()
        │
        ├─── 1. Update dates in IndexedDB (events store)
        │         await indexedDBManager.updateEventDates(...)
        │
        ├─── 2. Fetch full event from IndexedDB
        │         const fullEvent = await indexedDBManager.getEvent(eventId)
        │
        ├─── 3. Convert dates to ISO 8601
        │         const startDate = info.event.start.toISOString()
        │
        └─── 4. Queue sync operation (pendingOperations store)
                  await indexedDBManager.savePendingOperation({
                      type: 'PUT',
                      endpoint: '/api/events/21',
                      data: { 
                          title: fullEvent.title,      // ✅ Required!
                          startDate: startDate,          // ✅ ISO 8601!
                          // ... all other fields
                      },
                      token: localStorage.getItem('auth_token'),
                      timestamp: new Date().toISOString()
                  })
```

### Sync Flow When Back Online

```
Connection restored
        │
        ▼
[Reconnection Handler] handleOnline() or polling detects server
        │
        └─── Reload page with timestamp: /?_reconnect=1729221234567
                │
                ▼
[New Page Load]
        │
        ├─── 1. Blazor component initializes
        │         CalendarView.OnInitializedAsync()
        │
        ├─── 2. Connectivity service detects online
        │         ConnectivityService.InitializeAsync()
        │
        ├─── 3. OnConnectivityChanged triggers sync
        │         await SyncService.SyncAsync()
        │
        └─── 4. Process pending operations
                  │
                  ├─── Get operations: await LocalStorageService.GetPendingOperationsAsync()
                  │
                  ├─── For each operation:
                  │    │
                  │    ├─── Construct full URL
                  │    │    http://localhost:5006/api/events/21
                  │    │
                  │    ├─── Send PUT request with full event data
                  │    │    {
                  │    │      "title": "1 Test",
                  │    │      "startDate": "2025-10-26T09:00:00.000Z",
                  │    │      // ... all fields
                  │    │    }
                  │    │
                  │    └─── On success: Delete from pendingOperations
                  │
                  └─── Reload calendar with synced data ✅
```

---

## Testing Guidelines

### Manual Testing Checklist

#### ✅ **Test 1: Basic Offline Drag**
1. Load calendar while **online**
2. Verify events are visible
3. **Go offline** (DevTools Network → Offline)
4. **Wait 3-5 seconds** for connectivity detection
5. Verify console shows: `[Connectivity] 🔴 Connection lost`
6. **Drag "1 Test" event** to a new date
7. Verify console shows:
   - `🎯 Event X dropped - checking connection...`
   - `🔌 Connectivity manager reports offline` OR `🔌 Blazor circuit connected: false`
   - `⚠️ Circuit disconnected - using offline mode directly`
   - `💾 Saving event change offline...`
   - `✅ Event saved offline successfully, queued for sync`
8. Verify event moved on calendar
9. Open DevTools → Application → IndexedDB → EventSchedulerDB → pendingOperations
10. Verify new operation queued

**Expected Result**: Event moves immediately, saves to IndexedDB, queues for sync

---

#### ✅ **Test 2: Sync After Reconnection**
1. Complete Test 1 (drag event while offline)
2. **Go back online** (DevTools Network → No throttling)
3. Wait 2-5 seconds
4. Verify console shows:
   - `🟢 [SignalR] Server is back! Reloading page...`
5. Page reloads automatically
6. Verify event is in new position after reload
7. Open DevTools → Application → IndexedDB → pendingOperations
8. Verify queue is empty (operations processed)

**Expected Result**: Page reloads, event syncs, queue clears

---

#### ✅ **Test 3: Event Click Offline**
1. Load calendar while **online**
2. **Go offline** (DevTools Network → Offline)
3. **Wait 3-5 seconds**
4. **Click on any event**
5. Verify alert shows with cached data:
   - Title
   - Description
   - Location
   - Start/End dates
   - "⚠️ You're offline - full details and editing available when online"

**Expected Result**: Alert shows cached event data

---

#### ✅ **Test 4: Event Click Online**
1. **Refresh page** while **online**
2. **Click on any event**
3. Verify event details modal opens
4. Verify you can edit the event

**Expected Result**: Normal event editing works

---

#### ✅ **Test 5: Date Click Offline**
1. Load calendar while **online**
2. **Go offline**
3. **Wait 3-5 seconds**
4. **Click on empty date**
5. Verify alert shows:
   - "⚠️ You're offline"
   - "Creating new events is only available when online"
   - "You can still drag/resize existing events"

**Expected Result**: Informative alert, no errors

---

#### ✅ **Test 6: Multiple Offline Operations**
1. Load calendar while **online**
2. **Go offline**
3. **Wait 3-5 seconds**
4. **Drag event A** to new date
5. **Drag event B** to new date
6. **Drag event C** to new date
7. Open IndexedDB → pendingOperations
8. Verify **3 operations queued**
9. **Go back online**
10. Wait for auto-reload
11. Verify all 3 events in new positions
12. Verify pendingOperations is empty

**Expected Result**: All operations batch-sync successfully

---

#### ✅ **Test 7: Connection Loss During Drag**
**IMPORTANT**: This tests the window between offline and detection.

1. Load calendar while **online**
2. **Do NOT go offline yet**
3. **Start dragging event** (mouse down, moving)
4. **While still dragging**, go offline
5. **Drop event** on new date
6. Verify console shows timeout after 500ms:
   - `❌ Blazor save failed - switching to offline mode`
   - `💾 Saving event change offline...`

**Expected Result**: Even without connectivity detection, timeout catches it and saves offline

---

### Automated Testing Considerations

**Unit Tests Needed**:
```csharp
// Test offline service falls back to IndexedDB
[Fact]
public async Task UpdateEventAsync_WhenOffline_SavesToIndexedDB()
{
    // Arrange
    var mockApi = new Mock<ApiService>();
    mockApi.Setup(x => x.UpdateEventAsync(It.IsAny<int>(), It.IsAny<UpdateEventRequest>()))
           .ThrowsAsync(new HttpRequestException("Network unavailable"));
    
    var mockStorage = new Mock<LocalStorageService>();
    var service = new OfflineEventService(mockApi.Object, mockStorage.Object);
    
    // Act
    var result = await service.UpdateEventAsync(1, new UpdateEventRequest());
    
    // Assert
    mockStorage.Verify(x => x.SaveEventAsync(It.IsAny<EventResponse>()), Times.Once);
}
```

**Integration Tests Needed**:
- Test sync service processes pending operations correctly
- Test date format conversion in JavaScript
- Test full URL construction in service worker

---

## Troubleshooting

### Problem: Event clicks show offline alert even when online

**Symptom**: After reconnection, clicking events shows cached data alert instead of opening modal.

**Root Cause**: `isBlazorConnected` flag not restored to `true`.

**Solution**: 
1. Check console for `🔌 Connection restored - re-enabling Blazor calls`
2. If not showing, check `fullcalendar-interop.js` line 9-40
3. Ensure `checkBlazorConnection()` sets `isBlazorConnected = true` when all checks pass
4. Verify connectivity manager exposes `isOnline` getter property

**Quick Fix**:
```javascript
// In browser console:
window.fullCalendarInterop.isBlazorConnected = true;
```

---

### Problem: 400 Bad Request during sync

**Symptom**: Console shows "400 Bad Request" when syncing offline operations.

**Root Cause**: Date format incorrect (date-only instead of ISO 8601 datetime).

**Solution**:
1. Open DevTools → Application → IndexedDB → EventSchedulerDB → pendingOperations
2. Check operation data → `startDate` field
3. If format is "2025-10-26" (date only) → OLD FORMAT ❌
4. Should be "2025-10-26T09:00:00.000Z" (ISO 8601) → CORRECT ✅
5. Clear old operations: `await indexedDBManager.clearPendingOperations()`
6. Re-do operations with updated code

**Quick Fix**:
```javascript
// In browser console:
await window.indexedDBManager.clearPendingOperations();
console.log('Old operations cleared');
```

---

### Problem: 405 Method Not Allowed during sync

**Symptom**: Service worker shows "405 Method Not Allowed" for PUT requests.

**Root Cause**: Relative URLs being sent to wrong endpoint.

**Solution**:
1. Check `service-worker.js` line 195
2. Ensure full URL construction:
   ```javascript
   const apiBaseUrl = 'http://localhost:5006';
   const fullUrl = endpoint.startsWith('http') 
       ? endpoint 
       : `${apiBaseUrl}${endpoint}`;
   ```
3. Verify `pendingOperations` store has full URLs or proper relative paths

---

### Problem: Page freezes when going offline

**Symptom**: Calendar shows reconnection spinner and becomes unresponsive.

**Root Cause**: Blazor's default reconnection handler being called.

**Solution**:
1. Check `reconnection-handler.js` line 52-86
2. Ensure `origDown.call(handler)` is **NOT** called
3. Ensure modal is hidden immediately: `modal.className = 'components-reconnect-hide'`
4. Verify comment says "DON'T call Blazor's original handler"

---

### Problem: Drag works but save fails silently

**Symptom**: Event drags but no console messages about offline save.

**Root Cause**: Error detection not catching the specific error message.

**Solution**:
1. Check console for error message when drag fails
2. Update `fullcalendar-interop.js` line 152 to include that error pattern:
   ```javascript
   const isOfflineError = err.message && (
       err.message.includes('not in the \'Connected\' State') ||
       err.message.includes('Cannot send data') ||
       err.message.includes('connection') ||
       err.message.includes('YOUR_NEW_ERROR_PATTERN')
   );
   ```

---

## Known Limitations

### 1. **Creating Events Offline Not Supported**
- **Limitation**: New events can only be created when online
- **Reason**: Complex form validation and related data require API access
- **Workaround**: User sees informative message, can create when back online
- **Future**: Could implement offline creation with generated temp IDs

### 2. **Conflict Resolution: Last Write Wins**
- **Limitation**: If same event edited offline on multiple devices, last sync wins
- **Reason**: Simple conflict resolution strategy
- **Workaround**: None currently
- **Future**: Could implement proper conflict detection and user prompt

### 3. **Prerendering Errors in Logs**
- **Limitation**: Harmless errors during page prerendering
- **Reason**: Services try to call JavaScript before it's available
- **Workaround**: These are warnings, not errors - app works fine
- **Future**: Could disable prerendering for this component

### 4. **Service Worker Cache Size**
- **Limitation**: Browser may evict cache if storage pressure
- **Reason**: Browser storage management
- **Workaround**: App reloads data from server when cache missing
- **Future**: Could implement persistent storage request

### 5. **No Pessimistic UI Locking**
- **Limitation**: Other users' changes not locked when you edit offline
- **Reason**: No real-time presence detection offline
- **Workaround**: Last write wins
- **Future**: Could show sync conflicts and let user choose

---

## Future Enhancements

### Priority 1: High Value, Low Effort

1. **Better Offline Indicators**
   - Add persistent offline banner
   - Show sync queue count
   - Add manual sync button

2. **Retry Failed Syncs**
   - Exponential backoff for failed operations
   - Show failed operation count
   - Allow manual retry

### Priority 2: High Value, Medium Effort

3. **Offline Event Creation**
   - Generate temporary IDs
   - Create events locally
   - Map temp IDs to real IDs on sync

4. **Conflict Detection UI**
   - Detect conflicting edits
   - Show diff view
   - Let user choose which version to keep

### Priority 3: Nice to Have

5. **Operation Queue Management**
   - UI to view pending operations
   - Ability to delete pending operations
   - Re-order operation queue

6. **Advanced Sync Settings**
   - Choose sync strategy (automatic vs manual)
   - Sync only on WiFi
   - Sync scheduling

---

## Version History

- **v15** (2025-10-18): Auto-restore online state, multi-layer connection check
- **v14** (2025-10-18): Offline fallbacks for all click handlers
- **v13** (2025-10-18): Fast offline detection (500ms timeout)
- **v12** (2025-10-18): Circuit detection + pure JS offline mode
- **v11** (2025-10-18): Enhanced reconnection handler (no default Blazor handlers)
- **v10** (2025-10-18): ISO 8601 date format + complete event data sync
- **v2** (2025-10-18): Initial offline mode implementation

---

## Support & Debugging

### Enable Verbose Logging

**Browser Console**:
```javascript
// Enable detailed logging
localStorage.setItem('debug_offline', 'true');

// Check connectivity state
console.log('Connectivity:', {
    navigatorOnline: navigator.onLine,
    managerOnline: window.connectivityManager?.isOnline,
    blazorConnected: window.fullCalendarInterop?.isBlazorConnected
});

// Check pending operations
window.indexedDBManager.getPendingOperations().then(ops => {
    console.log('Pending operations:', ops.length);
    ops.forEach(op => console.log(op));
});
```

**Server Logs**:
```bash
# Watch logs in real-time
dotnet watch run --project EventScheduler.Api

# Filter for connectivity
dotnet watch run | grep -i "connectivity\|sync\|offline"
```

### Performance Monitoring

**Key Metrics**:
- Offline detection time: < 500ms
- IndexedDB save time: < 50ms
- Sync operation time: < 2s per operation
- Page reload time: < 3s

**Measure**:
```javascript
// Time offline detection
console.time('offline-detection');
// ... perform offline action
console.timeEnd('offline-detection');

// Monitor IndexedDB performance
performance.mark('idb-start');
await window.indexedDBManager.saveEvent(event);
performance.mark('idb-end');
performance.measure('idb-save', 'idb-start', 'idb-end');
console.log(performance.getEntriesByName('idb-save')[0].duration);
```

---

## Contact & Contribution

For questions, issues, or contributions related to offline mode:

1. Check this guide first
2. Review console logs for errors
3. Check IndexedDB state
4. Review `TROUBLESHOOTING.md` for common issues
5. Create detailed issue with:
   - Steps to reproduce
   - Console logs
   - IndexedDB state (screenshot)
   - Network conditions

---

**Last Updated**: October 18, 2025  
**Version**: 15.0  
**Author**: Development Team
