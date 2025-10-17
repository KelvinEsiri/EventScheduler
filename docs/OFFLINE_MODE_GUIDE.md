# Offline Mode Guide

## Overview

EventScheduler supports full offline functionality, allowing users to continue working with events when internet connectivity is unavailable. All changes made offline are automatically synchronized when the connection is restored.

## Features

### Core Capabilities
- **Work Without Internet**: Create, edit, and delete events offline
- **Automatic Synchronization**: Changes sync automatically when connection is restored  
- **Visual Status Indicators**: Connection status displayed in bottom-right corner
- **No Data Loss**: All offline changes are persisted in browser storage
- **Non-Blocking UI**: Reconnection spinner doesn't prevent interaction

### Supported Operations
- ✅ View cached events
- ✅ Create new events
- ✅ Update existing events (drag, resize, edit details)
- ✅ Delete events
- ✅ Navigate between pages

## Architecture

### Services

**OfflineStorageService**  
Manages IndexedDB operations for persistent storage:
- Stores events cache
- Manages pending operations queue
- Handles data serialization

**NetworkStatusService**  
Monitors connectivity:
- Detects browser online/offline events
- Performs periodic server health checks
- Notifies app of status changes

**OfflineSyncService**  
Orchestrates offline functionality:
- Coordinates data loading (online or cached)
- Queues offline operations
- Synchronizes pending changes when online

### Storage

**IndexedDB Stores:**

1. `events` - Cached event data
   - Key: event.id
   - Enables offline viewing

2. `pendingOperations` - Queued changes
   - Key: operation.Id (timestamp)
   - Indexed by: timestamp, eventId
   - Operations: create, update, delete

3. `conflicts` - Detected conflicts (for future use)
   - Key: auto-increment
   - Indexed by: eventId, timestamp

## User Experience

### Visual Indicators

**Connection Status (Bottom-Right Corner):**
- 🟢 **Green wifi icon** - Online and connected
- 🔴 **Red wifi-off icon** - Offline mode
- 🔄 **Blue spinning icon** - Syncing changes
- **Badge** - Shows count of pending operations

### Workflow

**Going Offline:**
1. Network connection drops
2. Status indicator turns red
3. Current events cached to IndexedDB
4. Brief toast notification appears
5. All features remain functional

**Working Offline:**
1. User creates/edits/deletes events normally
2. Changes saved to IndexedDB immediately
3. Operations queued for later sync
4. Pending count increments
5. Toast confirms "Saved offline"

**Reconnecting:**
1. Connection restored automatically
2. Status indicator shows "Syncing..."
3. Pending operations processed in order
4. Fresh data fetched from server
5. UI updates automatically
6. Success toast shown

## Technical Details

### Data Flow

**Online Mode:**
```
User Action → ApiService → Backend API → Cache + UI Update
```

**Offline Mode:**
```
User Action → OfflineSyncService → IndexedDB → Pending Queue → UI Update
```

**Reconnection:**
```
Network Restored → Process Pending → Sync with API → Update Cache → Refresh UI
```

### Operation Queuing

When offline, operations are stored with complete event data:

```javascript
{
  Id: 1234567890000,           // Timestamp
  Type: 'update',              // create, update, delete
  EventId: 42,                 // Event being modified
  Data: '{"Title":"..."}',     // Complete UpdateEventRequest JSON
  Timestamp: '2025-10-17T...'  // ISO 8601 timestamp
}
```

### Synchronization Logic

1. **Check Connection**: Verify network is available
2. **Retrieve Queue**: Get pending operations from IndexedDB
3. **Sort by Time**: Process operations in chronological order
4. **Execute Operations**: Send each to API
5. **Remove on Success**: Clear completed operations
6. **Retry on Failure**: Keep failed operations for next sync
7. **Refresh Data**: Fetch latest events from server
8. **Update Cache**: Save fresh data to IndexedDB

### Conflict Resolution

Current implementation uses "last write wins" strategy:
- Operations processed in timestamp order
- Server always has final state
- No merge logic for concurrent edits
- Future enhancement: conflict detection UI

## Browser Compatibility

Requires IndexedDB support (all modern browsers):
- ✅ Chrome/Edge 24+
- ✅ Firefox 16+
- ✅ Safari 10+
- ✅ Mobile browsers (iOS 10+, Android 4.4+)

## Known Limitations

1. **No Real-Time Updates Offline**  
   SignalR requires active connection. Changes from other users won't appear until online.

2. **Simple Conflict Resolution**  
   Last write wins - no merge logic for concurrent edits.

3. **Authentication Required**  
   User must be logged in before going offline. Tokens expire after 8 hours.

4. **Storage Limits**  
   IndexedDB typically allows 50MB+ per origin, but varies by browser.

## Testing

### Manual Testing

**Simulate Offline Mode:**
1. Open browser DevTools (F12)
2. Navigate to Network tab
3. Select "Offline" from throttling dropdown
4. Create/edit/delete events
5. Switch back to "No throttling"
6. Verify changes sync automatically

**Test Scenarios:**
- Create event offline → Go online → Verify created
- Edit event offline → Go online → Verify updated
- Delete event offline → Go online → Verify deleted
- Make multiple changes → Go online → Verify all synced
- Reload page while offline → Verify cached events display

### Console Monitoring

**Key Log Messages:**

```
[NetworkStatus] Status: OFFLINE
[OfflineStorage] Saved 15 events to offline storage
[OfflineSync] Event queued for creation offline
[NetworkStatus] Status: ONLINE
[OfflineSync] Found 3 pending operations to sync
[OfflineSync] Synced operation: create
[OfflineSync] Synchronization completed: 3 operations synced successfully
```

### Inspecting IndexedDB

1. Open DevTools → Application tab
2. Expand IndexedDB → EventSchedulerOfflineDB
3. View stores:
   - `events` - Cached event data
   - `pendingOperations` - Queued changes
   - `conflicts` - Detected conflicts (if any)

## Troubleshooting

### Events Not Loading Offline

**Solution:**
- Check browser console for errors
- Verify IndexedDB is enabled
- Clear browser cache and reload
- Check storage quota not exceeded

### Sync Not Working

**Solution:**
- Verify connection status indicator shows "Online"
- Check pending operations count
- Review console for sync errors
- Ensure authentication token is valid
- Try manual page refresh

### Pending Operations Stuck

**Solution:**
- Open DevTools → Application → IndexedDB
- Check `pendingOperations` store for entries
- Review operation data for errors
- If necessary, manually clear: `await offlineStorage.clearAll()`

### Calendar Not Updating After Reconnect

**Solution:**
- Check console for JavaScript errors
- Verify calendar instance exists
- Hard refresh page (Ctrl+Shift+R)
- Clear browser cache if issue persists

## Security Considerations

1. **Local Storage**  
   Events cached in browser using IndexedDB. Sensitive data visible to anyone with device access.

2. **Token Expiration**  
   JWT tokens expire after 8 hours. User must re-authenticate if offline longer.

3. **HTTPS Required**  
   Use HTTPS in production to protect data in transit.

4. **Clear on Logout**  
   Offline cache cleared when user logs out (recommended future enhancement).

## Future Enhancements

- [ ] Conflict resolution UI for concurrent edits
- [ ] Selective sync (choose which operations to sync)
- [ ] Background sync using Service Workers
- [ ] Progressive Web App (PWA) support
- [ ] Offline authentication with token refresh
- [ ] Data compression for storage efficiency
- [ ] Export offline data for backup

## Related Documentation

- [Architecture](ARCHITECTURE.md) - System design and patterns
- [Database Setup](DATABASE_SETUP.md) - Backend database configuration
- [Logging Guide](LOGGING_GUIDE.md) - Troubleshooting with logs

---

**Last Updated**: 2025-10-17  
**Version**: 1.0
