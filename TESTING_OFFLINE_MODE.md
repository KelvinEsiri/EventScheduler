# Testing Guide: Offline Mode Fix

This guide explains how to test the offline mode implementation fix to verify that users can navigate between pages while offline without seeing the reconnection spinner.

## Prerequisites

1. Start both API and Web servers:
   ```bash
   # Terminal 1 - API Server
   cd EventScheduler.Api
   dotnet run
   
   # Terminal 2 - Web Server  
   cd EventScheduler.Web
   dotnet run
   ```

2. Open browser and navigate to `http://localhost:5292`

3. Login to the application

## Test Scenarios

### Scenario 1: Offline Navigation (PRIMARY FIX)

**Expected Behavior**: Navigate between pages while offline WITHOUT seeing reconnection spinner

**Steps:**
1. Login and navigate to Calendar View (`/calendar-view`)
2. Open Chrome DevTools (F12)
3. Go to Network tab
4. Set throttling to "Offline"
5. Click navigation links: Home → Calendar List → Calendar View → Public Events
6. Navigate back and forth multiple times

**✅ SUCCESS**: You can navigate between pages without seeing the "Attempting to reconnect to the server..." spinner

**❌ FAIL**: Reconnection spinner appears when navigating offline

**Console Output (Expected)**:
```
📴 [SignalR] User is offline - skipping reconnection UI
```

---

### Scenario 2: Create Event Offline

**Expected Behavior**: Create events while offline, they should queue for sync

**Steps:**
1. Set browser to Offline mode (DevTools → Network → Offline)
2. Go to Calendar View
3. Click "New Event" button
4. Fill in event details:
   - Title: "Test Offline Event"
   - Date/Time: Tomorrow at 10 AM
5. Click "Create"

**✅ SUCCESS**: 
- Event appears in calendar immediately
- Toast message: "Event created offline - will sync when online"
- Event stored in IndexedDB

**❌ FAIL**: 
- Error message appears
- Event doesn't show in calendar

**Console Output (Expected)**:
```
[OfflineStorage] Added pending operation: create for event null
[OfflineStorage] Saved X events to offline storage
```

---

### Scenario 3: Edit Event Offline

**Expected Behavior**: Edit existing events while offline, changes queue for sync

**Steps:**
1. Ensure some events exist in calendar
2. Set browser to Offline mode
3. Click on an event
4. Click "Edit" button
5. Change the title to "Updated Offline"
6. Save changes

**✅ SUCCESS**:
- Event updates immediately in UI
- Toast message: "Event updated offline - will sync when online"
- Change stored in IndexedDB

**❌ FAIL**:
- Error message appears
- Event doesn't update

---

### Scenario 4: Delete Event Offline

**Expected Behavior**: Delete events while offline, deletion queues for sync

**Steps:**
1. Set browser to Offline mode
2. Click on an event in calendar
3. Click "Delete" button
4. Confirm deletion

**✅ SUCCESS**:
- Event disappears from calendar immediately
- Toast message: "Event deleted offline - will sync when online"
- Deletion stored in IndexedDB

**❌ FAIL**:
- Error message appears
- Event doesn't disappear

---

### Scenario 5: Sync When Back Online

**Expected Behavior**: All offline changes sync automatically when network is restored

**Steps:**
1. While offline, create 2 events, edit 1 event, delete 1 event
2. Go to DevTools → Network tab
3. Change throttling from "Offline" to "No throttling"
4. Wait a few seconds

**✅ SUCCESS**:
- Console shows: "[OfflineSync] Synchronizing pending changes..."
- Console shows: "[OfflineSync] Synced operation: create"
- All changes appear in other browser tabs/windows
- SignalR reconnects

**❌ FAIL**:
- Changes don't sync
- Errors in console

**Console Output (Expected)**:
```
[NetworkStatus] Network status changed to Online
[OfflineSync] Synchronizing pending changes...
[OfflineSync] Found X pending operations to sync
[OfflineSync] Synced operation: create
[OfflineSync] Synced operation: update
[OfflineSync] Synced operation: delete
[OfflineSync] Synchronization completed successfully
🟢 [SignalR] Connection restored!
```

---

### Scenario 6: Server Down (But Online)

**Expected Behavior**: Reconnection spinner SHOULD show when server is down but user is online

**Steps:**
1. Ensure browser is NOT in offline mode
2. Stop the API server (Ctrl+C in API terminal)
3. Try to navigate to a different page

**✅ SUCCESS**:
- Reconnection spinner appears: "Attempting to reconnect to the server..."
- Console shows polling attempts
- When API restarts, page reloads automatically

**❌ FAIL**:
- No spinner appears (should appear in this case!)

**Console Output (Expected)**:
```
🔴 [SignalR] Connection lost (online) - starting active polling
📡 [SignalR] Polling server...
❌ [SignalR] Server still down, will retry...
📡 [SignalR] Polling server...
🟢 [SignalR] Server is back! Reloading page...
```

---

### Scenario 7: Drag & Drop Offline

**Expected Behavior**: Move events by dragging while offline

**Steps:**
1. Set browser to Offline mode
2. Go to Calendar View
3. Drag an event to a different date/time
4. Drop the event

**✅ SUCCESS**:
- Event moves immediately in calendar
- Toast message: "✅ Event moved to [new date] (offline - will sync)"
- Change queued in IndexedDB

**❌ FAIL**:
- Event reverts to original position
- Error message appears

---

### Scenario 8: View Cached Events

**Expected Behavior**: View previously loaded events while offline

**Steps:**
1. While online, load Calendar View (events are cached)
2. Set browser to Offline mode
3. Reload the page (Ctrl+R)
4. Navigate to Calendar List and back to Calendar View

**✅ SUCCESS**:
- Events load from cache
- All previously viewed events are visible
- Can interact with cached events

**❌ FAIL**:
- No events show
- Loading spinner never stops
- Error messages

**Console Output (Expected)**:
```
[OfflineStorage] Retrieved X events from offline storage
[OfflineSync] Offline mode, loading from cache
```

---

## Monitoring Tools

### Browser Console
Press F12 and monitor these logs:
- `[SignalR]` - Connection status and reconnection attempts
- `[OfflineStorage]` - IndexedDB operations
- `[OfflineSync]` - Synchronization status
- `[NetworkStatus]` - Online/offline status changes

### IndexedDB Inspector
1. Press F12
2. Go to "Application" tab
3. Expand "IndexedDB"
4. Expand "EventSchedulerOfflineDB"
5. View:
   - `events` store - Cached events
   - `pendingOperations` store - Queued changes

### Network Tab
1. Press F12
2. Go to "Network" tab
3. Use throttling dropdown to simulate offline:
   - "Offline" - No network at all
   - "Slow 3G" - Test slow connections
   - "No throttling" - Normal speed

---

## Expected User Experience

### When Online (Normal)
- Events load instantly from server
- Changes save immediately
- Real-time updates via SignalR
- Green wifi icon in header
- No "offline" messages

### When Offline
- Events load from cache
- Can create/edit/delete events
- Changes queued for sync
- Red wifi-off icon in header
- Messages indicate "offline - will sync"
- **NO reconnection spinner when navigating**

### When Reconnecting
- Spinner shows if server is down (but online)
- Auto-sync starts when network restored
- Fresh data downloaded
- SignalR reconnects
- Real-time updates resume

---

## Troubleshooting

### Issue: Events not loading offline
**Solution**: 
1. Check IndexedDB has data: F12 → Application → IndexedDB
2. Verify events were loaded while online first
3. Clear browser cache and try again

### Issue: Changes not syncing when online
**Solution**:
1. Check console for sync errors
2. Verify API server is running
3. Check pending operations: F12 → Application → IndexedDB → pendingOperations

### Issue: Reconnection spinner showing offline
**Solution**:
1. Verify browser is in offline mode: DevTools → Network → Offline
2. Check console for `navigator.onLine` value
3. This is the bug we fixed - if you see this, the fix didn't apply

### Issue: Can't navigate while offline
**Solution**:
1. This is the main bug we fixed
2. Verify reconnection-handler.js has the offline check
3. Check browser console for network status logs

---

## Success Criteria

All these should work:
- ✅ Navigate between pages offline without spinner
- ✅ Create events offline
- ✅ Edit events offline
- ✅ Delete events offline
- ✅ Drag/drop events offline
- ✅ Auto-sync when online
- ✅ Spinner shows when server down (but online)
- ✅ Load cached events offline
- ✅ Proper user feedback for offline operations

---

## Notes

- The fix is **client-side only** - no database changes needed
- Browser must support IndexedDB (all modern browsers do)
- Must be logged in before going offline (no offline auth)
- Conflict resolution is simple: last write wins
- SignalR requires connection - no real-time updates offline

---

For more details, see: `OFFLINE_MODE_FIX.md`
