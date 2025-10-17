# Offline Functionality Testing Guide

## Overview
This guide provides step-by-step instructions for testing the new offline-first functionality in EventScheduler.

## Prerequisites
- Modern browser (Chrome, Edge, Firefox, Safari)
- Developer Tools access (F12)
- EventScheduler API and Web servers running

## Test Scenarios

### 1. Service Worker Installation

**Steps:**
1. Start the application (API on port 5005, Web on port 5292)
2. Open browser DevTools (F12)
3. Navigate to Application → Service Workers (Chrome/Edge) or Storage → Service Workers (Firefox)
4. Verify "service-worker.js" is registered and running

**Expected Results:**
- Service Worker status: "activated and is running"
- Scope: "/"
- Console shows: "[ServiceWorker] Registered successfully"

---

### 2. Offline Event Creation

**Steps:**
1. Login to the application
2. Navigate to Calendar View
3. Open DevTools Network tab
4. Select "Offline" from throttling dropdown
5. Observe connection indicator in bottom-right corner (should show "Offline" with pending count)
6. Click "New Event" button
7. Fill in event details:
   - Title: "Test Offline Event"
   - Start Date: Tomorrow at 10:00 AM
   - End Date: Tomorrow at 11:00 AM
8. Click "Create"

**Expected Results:**
- Event appears on calendar immediately (optimistic UI)
- Toast notification: "Event created offline - will sync when online"
- Connection indicator shows "1 change pending"
- Event is visible in calendar with temporary ID
- No errors in console

---

### 3. Offline Event Update

**Steps:**
1. While still offline, click on the event created in Test 2
2. In the details modal, click "Edit"
3. Change the title to "Updated Offline Event"
4. Change the time to 2:00 PM - 3:00 PM
5. Click "Update"

**Expected Results:**
- Changes appear immediately on calendar
- Toast: "Event updated offline - will sync when online"
- Connection indicator shows "2 changes pending"
- Event details reflect new values

---

### 4. Offline Event Deletion

**Steps:**
1. While still offline, click on an existing event
2. Click "Delete" in the modal
3. Confirm deletion

**Expected Results:**
- Event removed from calendar immediately
- Toast: "Event deleted offline - will sync when online"
- Connection indicator shows "3 changes pending"
- Event no longer visible in list

---

### 5. Sync When Back Online

**Steps:**
1. With pending changes from Tests 2-4
2. In DevTools Network tab, change from "Offline" to "No throttling"
3. Watch the connection indicator

**Expected Results:**
- Connection indicator shows "Syncing..." briefly
- Icon animates (pulsing)
- After 2-3 seconds, indicator shows "Online" with "0 changes pending"
- All events refresh from server
- Console shows:
  ```
  [OfflineSync] Synchronizing pending changes...
  [OfflineSync] Synced operation: create
  [OfflineSync] Synced operation: update
  [OfflineSync] Synced operation: delete
  [OfflineSync] Synchronization completed successfully
  ```

---

### 6. Offline Navigation

**Steps:**
1. Set Network tab to "Offline"
2. Navigate between pages:
   - Calendar View → Calendar List
   - Calendar List → Public Events
   - Public Events → Calendar View

**Expected Results:**
- All pages load successfully from cache
- No "Attempting to reconnect..." modal blocks navigation
- Connection indicator shows "Offline" on all pages
- Cached events display correctly
- No errors in console

---

### 7. Service Worker Cache

**Steps:**
1. With network online, refresh the page
2. Open DevTools → Application → Cache Storage
3. Expand "eventscheduler-v1"
4. Verify cached resources

**Expected Results:**
- CSS files cached (main.css, calendar.css, etc.)
- JavaScript files cached (offline-storage.js, network-status.js, etc.)
- FullCalendar CDN resources cached
- Bootstrap Icons cached

---

### 8. Reconnection Modal (Server Down)

**Steps:**
1. Stop the API server (but keep browser online)
2. Try to create/update an event
3. Observe the reconnection modal

**Expected Results:**
- Small modal appears in bottom-right corner (not fullscreen)
- Shows "Attempting to reconnect to the server..."
- Spinner is visible
- Modal is non-blocking (can still interact with page)
- Position: bottom-right, just above connection indicator

---

### 9. Mobile Responsiveness

**Steps:**
1. Open DevTools → Toggle device toolbar (Ctrl+Shift+M)
2. Select iPhone SE (375px width)
3. Test offline functionality:
   - Create event while offline
   - Verify connection indicator is visible and readable
   - Check that it doesn't overlap with other UI elements

**Expected Results:**
- Connection indicator scaled appropriately
- Text remains readable
- Positioned in bottom-right corner
- Doesn't interfere with calendar or buttons
- Modal dialogs scale properly

---

### 10. Conflict Detection (Advanced)

**Steps:**
1. Open two browser windows with the same account
2. Create an event "Conflict Test" in Window 1 (online)
3. Go offline in Window 1 (DevTools → Network → Offline)
4. Edit "Conflict Test" in Window 1 (change title to "Offline Edit")
5. Edit "Conflict Test" in Window 2 (online) (change title to "Online Edit")
6. Go back online in Window 1
7. Wait for sync to complete

**Expected Results:**
- Window 1 syncs changes to server
- Last-write-wins applied (Window 1's changes win)
- Conflict logged in console
- Both windows eventually show same data after refresh
- No data loss or errors

---

## Browser Console Monitoring

### Key Log Messages to Watch For:

**Service Worker:**
```
[ServiceWorker] Installing...
[ServiceWorker] Caching app shell
[ServiceWorker] Activating...
[ServiceWorker] Registered successfully
```

**Network Status:**
```
[NetworkStatus] Network status changed: Offline
[NetworkStatus] Network status changed: Online
```

**Offline Operations:**
```
[OfflineStorage] Saved 15 events to offline storage
[OfflineSync] Event queued for creation offline
[OfflineSync] Event queued for update offline
[OfflineSync] Event queued for deletion offline
```

**Synchronization:**
```
[OfflineSync] Synchronizing pending changes...
[OfflineSync] Found 3 pending operations to sync
[OfflineSync] Synced operation: create
[OfflineSync] Synced operation: update
[OfflineSync] Synced operation: delete
[OfflineSync] Synchronization completed successfully
```

---

## Troubleshooting

### Service Worker Not Registering
**Solution:** 
- Check browser console for errors
- Clear browser cache (Ctrl+Shift+Delete)
- Ensure app is served over HTTPS or localhost
- Check DevTools → Application → Service Workers for errors

### Events Not Syncing
**Solution:**
- Check IndexedDB (DevTools → Application → IndexedDB → EventSchedulerDB)
- Verify "pendingOperations" store has entries
- Check network connectivity
- Review console for sync errors
- Try manual refresh

### Connection Indicator Not Showing
**Solution:**
- Ensure NetworkStatusService is initialized
- Check browser console for JavaScript errors
- Verify component is rendered (inspect DOM)
- Check CSS is loaded correctly

### Offline Modal Still Blocking Screen
**Solution:**
- Hard refresh (Ctrl+Shift+R)
- Clear browser cache
- Check CSS file is latest version
- Inspect element to verify classes applied

---

## IndexedDB Inspection

To verify offline storage is working:

1. Open DevTools → Application → IndexedDB
2. Expand "EventSchedulerDB"
3. Check stores:
   - **events**: Should contain cached events
   - **pendingOperations**: Should contain queued changes when offline
   - **conflicts**: Should contain detected conflicts (usually empty)

---

## Performance Notes

- First load: ~2-3 seconds (downloading and caching)
- Subsequent loads: < 1 second (from cache)
- Sync time: < 2 seconds for typical number of operations
- IndexedDB operations: < 100ms

---

## Known Limitations

1. **No real-time updates offline**: SignalR requires connection
2. **Simple conflict resolution**: Last-write-wins strategy
3. **No offline authentication**: Must be logged in before going offline
4. **Browser storage limits**: IndexedDB typically 50MB+ depending on browser

---

## Reporting Issues

If you encounter issues during testing:

1. Include browser name and version
2. Provide console logs (especially errors)
3. Screenshot of the issue
4. Steps to reproduce
5. Expected vs actual behavior

---

**Testing completed by:** _________________  
**Date:** _________________  
**Browser:** _________________  
**All tests passed:** ☐ Yes ☐ No  
**Issues found:** _________________
