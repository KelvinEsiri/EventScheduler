# Calendar View Offline Support - Testing Guide

## Overview
The Calendar View page now has complete offline support, allowing users to create, edit, delete, and view events even when disconnected from the internet.

## Features Added
- ✅ Offline-first event loading with IndexedDB cache
- ✅ Create events while offline (queued for sync)
- ✅ Update events while offline (drag, drop, resize)
- ✅ Delete events while offline
- ✅ Automatic sync when connection is restored
- ✅ Visual offline/online indicator
- ✅ Pending operations counter
- ✅ User-friendly offline status messages

## How to Test

### 1. Test Online Mode (Baseline)
1. Start the API and Web servers
2. Login to the application
3. Navigate to Calendar View (`/calendar-view`)
4. Verify you see the WiFi icon (connected)
5. Create a new event - should save immediately
6. Edit an event by dragging/resizing - should update immediately
7. Delete an event - should remove immediately

### 2. Test Offline Event Creation
1. While on Calendar View, open Browser DevTools (F12)
2. Go to **Network** tab
3. Change throttling to **Offline**
4. Verify WiFi icon changes to WiFi-off (disconnected)
5. Click "New Event" and create an event
6. Event should be created with success message "(will sync when online)"
7. Event appears on calendar immediately
8. Switch back to **Online** in Network tab
9. Watch for automatic sync message
10. Verify event persists after page refresh

### 3. Test Offline Event Updates
1. Set browser to **Offline** mode
2. Drag an event to a different date/time
3. Success message should say "(will sync when online)"
4. Event position updates immediately on calendar
5. Resize an event's duration
6. Event duration updates immediately
7. Switch back to **Online**
8. Auto-sync should trigger
9. Refresh page - changes should persist

### 4. Test Offline Event Deletion
1. Set browser to **Offline** mode
2. Click an event to open details
3. Click Delete button
4. Confirm deletion
5. Event disappears from calendar
6. Message shows "(will sync when online)"
7. Switch back to **Online**
8. Auto-sync should trigger
9. Refresh page - event should remain deleted

### 5. Test Mixed Operations
1. Start **Offline**
2. Create 2 new events
3. Edit 1 existing event
4. Delete 1 existing event
5. Switch to **Online**
6. Watch sync process - should show "Synced X events and Y operations"
7. Refresh page
8. All changes should persist

### 6. Test Cache Persistence
1. While **Online**, load Calendar View
2. Create several events
3. Set browser to **Offline**
4. Refresh the page
5. All events should still be visible (loaded from cache)
6. Message should indicate offline mode

### 7. Test Reconnection
1. Start on Calendar View while **Offline**
2. Switch to **Online**
3. Observe:
   - WiFi icon changes from off to on
   - "Syncing..." message appears
   - "Synced X events and Y operations" message shows
   - Calendar refreshes with latest data

### 8. Test OfflineIndicator Component
1. Navigate to Calendar View
2. Look for the offline indicator at top of page (from MainLayout)
3. Set browser to **Offline**
4. Yellow "Offline Mode" banner should appear
5. If there are pending operations, it shows count
6. Switch to **Online**
7. Blue "Syncing..." banner appears
8. Green "Synced" banner appears briefly
9. Banner disappears after 3 seconds

## Browser DevTools Testing Methods

### Method 1: Network Tab (Recommended)
1. Open DevTools (F12)
2. Network tab
3. Throttling dropdown → "Offline"
4. Test functionality
5. Throttling dropdown → "Online" or "No throttling"

### Method 2: Application Tab (Service Worker)
1. Open DevTools (F12)
2. Application tab
3. Service Workers section
4. Check "Offline" checkbox
5. Test functionality
6. Uncheck "Offline"

### Method 3: Console (Quick Toggle)
```javascript
// Simulate offline
window.dispatchEvent(new Event('offline'));

// Simulate online
window.dispatchEvent(new Event('online'));
```

## Expected Behavior

### When Going Offline
- WiFi icon changes to WiFi-off
- Connection status shows "Offline"
- Events load from IndexedDB cache
- All CRUD operations work locally
- Success messages include "(will sync when online)"

### When Coming Online
- WiFi icon changes to WiFi
- Connection status shows "Syncing..."
- Pending operations are processed
- Local and server data are merged
- Success message shows sync results
- Calendar refreshes with latest data

### Error Handling
- Failed operations remain in queue
- Retry on next sync attempt
- User-friendly error messages
- No data loss

## Offline Indicator States

1. **Online (Default)**: WiFi icon, no banner
2. **Offline**: WiFi-off icon, yellow "Offline Mode" banner
3. **Syncing**: WiFi icon, blue "Syncing..." banner with spinner
4. **Just Synced**: WiFi icon, green "Synced" banner (auto-hides after 3s)

## Success Messages

### Online Mode
- "Event created successfully!"
- "Event updated successfully!"
- "Event deleted successfully!"
- "✅ Event moved to [date]"
- "✅ Duration updated to [duration]"

### Offline Mode
- "Event created successfully! (will sync when online)"
- "Event updated successfully! (will sync when online)"
- "Event deleted successfully! (will sync when online)"
- "✅ Event moved to [date] (will sync when online)"
- "✅ Duration updated to [duration] (will sync when online)"

## Known Behaviors

1. **Initial Load**: Page must be loaded while online first (Blazor Server requirement)
2. **Temporary IDs**: Events created offline have negative IDs until synced
3. **SignalR**: Real-time updates only work when online
4. **Conflict Resolution**: Last-write-wins based on timestamps
5. **Cache**: Cleared on logout for security

## Troubleshooting

### Events not syncing?
1. Check browser console for errors
2. Verify service worker is active (DevTools → Application → Service Workers)
3. Check pending operations count in offline indicator
4. Try manual page refresh

### Calendar not updating after sync?
1. Check browser console for JavaScript errors
2. Verify FullCalendar is initialized
3. Try refreshing the page
4. Check that events are in correct format

### Offline indicator not showing?
1. Verify MainLayout includes `<OfflineIndicator />`
2. Check that ConnectivityService is initialized
3. Look for errors in browser console
4. Verify connectivity-manager.js is loaded

## Performance Notes

- IndexedDB operations are asynchronous (non-blocking)
- Sync runs in background
- Optimistic UI updates for instant feedback
- Debounced connectivity checks (30 seconds)

## Security Notes

- JWT token included in pending operations
- All operations authenticated
- Cache cleared on logout
- HTTPS required for service workers

## Browser Compatibility

- ✅ Chrome 80+ (full support)
- ✅ Edge 80+ (full support)
- ✅ Firefox 75+ (full support)
- ⚠️ Safari 15+ (no Background Sync, uses immediate sync)

## Related Documentation

- [OFFLINE_SUPPORT_ARCHITECTURE.md](OFFLINE_SUPPORT_ARCHITECTURE.md) - System architecture
- [OFFLINE_QUICK_REFERENCE.md](OFFLINE_QUICK_REFERENCE.md) - Quick reference guide
- [OFFLINE_SUPPORT_GUIDE.md](OFFLINE_SUPPORT_GUIDE.md) - Developer guide
