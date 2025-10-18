# Event List Offline Support Guide

## Overview

The Event List page (`/calendar-list`) now has **full offline support**, allowing users to view, create, edit, and delete events even when the internet connection is unavailable. All changes are automatically cached locally and synchronized with the server when connectivity is restored.

---

## Features

### ✅ Offline Capabilities

1. **View Events Offline**
   - Browse all events in list format
   - View complete event details
   - Access previously loaded events from IndexedDB cache

2. **Filter & Search Offline**
   - Filter by event type (Festival, Interview, Birthday, etc.)
   - Filter by status (Scheduled, InProgress, Late, Completed, Cancelled)
   - Search events by title or description
   - Toggle between Active Events and History tabs
   - All filtering happens on cached data

3. **Create Events Offline**
   - Create new events with full details
   - Events are saved to IndexedDB
   - Creation operation queued for server sync
   - Temporary IDs assigned until synced

4. **Edit Events Offline**
   - Modify existing event details
   - Changes saved to local cache
   - Update operation queued for server sync

5. **Delete Events Offline**
   - Remove events from local cache
   - Deletion operation queued for server sync

### 🔄 Automatic Synchronization

When connectivity is restored:
- All queued operations are processed in order
- Local cache is updated with latest server data
- Conflicts resolved using last-write-wins strategy
- User notified of sync completion

---

## User Experience

### Visual Indicators

1. **Loading State**
   - When online: "Loading Your Events - Getting everything ready for you..."
   - When offline: "Attempting to reconnect to the server... - Please wait while we restore your connection"

2. **Offline Status Indicator** (Top-right corner)
   - 🟡 **Offline Mode** - Yellow badge with pending operation count
   - 🔵 **Syncing** - Blue badge with spinner
   - ✅ **Synced** - Green badge (shown briefly after sync)

3. **Navigation Menu**
   - Calendar View and Event List show ✓ checkmark when offline
   - Other pages show 🚫 disabled badge when offline

### Seamless Navigation

Users can freely switch between:
- ✅ Calendar View (`/calendar-view`)
- ✅ Event List (`/calendar-list`)

Without losing any offline functionality.

---

## Technical Implementation

### Architecture

```
CalendarList.razor (UI)
        │
        ├─► OfflineEventService (Offline-first CRUD)
        │           │
        │           ├─► ApiService (When online)
        │           └─► LocalStorageService (When offline)
        │                       │
        │                       └─► IndexedDB Manager (JavaScript)
        │
        ├─► ConnectivityService (Monitor network state)
        │           │
        │           └─► Connectivity Manager (JavaScript)
        │
        └─► SyncService (Background sync)
                    │
                    └─► Process pending operations when online
```

### Key Services

#### OfflineEventService
Handles all event CRUD operations with automatic fallback to local storage when offline.

**Methods**:
- `GetEventsAsync()` - Load events (server → cache fallback)
- `GetEventAsync(id)` - Get single event (server → cache fallback)
- `CreateEventAsync(request)` - Create event (server or queue)
- `UpdateEventAsync(id, request)` - Update event (server or queue)
- `DeleteEventAsync(id)` - Delete event (server or queue)

#### ConnectivityService
Monitors network connectivity and notifies components of state changes.

**Events**:
- `ConnectivityChanged` - Fired when network state changes
- Provides `IsOnline` property for current state

#### SyncService
Manages synchronization of offline changes when connectivity is restored.

**Events**:
- `SyncStarted` - Fired when sync begins
- `SyncCompleted` - Fired when sync finishes (with result)

**Methods**:
- `SyncAsync()` - Perform full synchronization
- `QueueOperationAsync()` - Queue operation for later sync
- `GetPendingOperationsCountAsync()` - Get count of pending operations

---

## Code Examples

### Load Events (Offline-First)

```csharp
private async Task LoadEvents()
{
    try
    {
        isLoading = true;
        
        // OfflineEventService automatically handles online/offline
        events = await OfflineEventService.GetEventsAsync();
        FilterEvents();
        
        Logger.LogInformation("Loaded {Count} events", events.Count);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to load events");
    }
    finally
    {
        isLoading = false;
    }
}
```

### Create Event Offline

```csharp
private async Task SaveEvent()
{
    try
    {
        if (isEditMode)
        {
            // Update existing event
            await OfflineEventService.UpdateEventAsync(editEventId, updateRequest);
        }
        else
        {
            // Create new event
            await OfflineEventService.CreateEventAsync(eventRequest);
        }
        
        CloseModal();
        await LoadEvents(); // Refresh list from cache
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to save event");
        await JSRuntime.InvokeVoidAsync("alert", "Failed to save event.");
    }
}
```

### Handle Connectivity Changes

```csharp
protected override async Task OnInitializedAsync()
{
    // Initialize connectivity monitoring
    await ConnectivityService.InitializeAsync();
    isConnected = ConnectivityService.IsOnline;
    
    // Subscribe to connectivity changes
    ConnectivityService.ConnectivityChanged += OnConnectivityChanged;
    
    // Subscribe to sync events
    SyncService.SyncStarted += OnSyncStarted;
    SyncService.SyncCompleted += OnSyncCompleted;
}

private void OnConnectivityChanged(object? sender, bool isOnline)
{
    isConnected = isOnline;
    
    InvokeAsync(async () =>
    {
        if (isOnline)
        {
            // Reload events from server when back online
            await LoadEvents();
        }
        StateHasChanged();
    });
}
```

---

## Testing Guidelines

### Manual Testing Checklist

#### ✅ Test 1: Basic Offline Viewing
1. Load Event List page while online
2. Verify events load correctly
3. Disconnect network (Chrome DevTools → Network → Offline)
4. Refresh page
5. Verify events still visible from cache
6. Verify "Offline Mode" indicator appears in top-right corner

#### ✅ Test 2: Offline Filtering
1. Start offline (with cached events)
2. Change event type filter
3. Change status filter
4. Enter search query
5. Toggle between Active/History tabs
6. Verify all filtering works on cached data

#### ✅ Test 3: Offline Event Creation
1. Start offline
2. Click "New Event" button
3. Fill in event details
4. Save event
5. Verify event appears in list
6. Check browser console - should see operation queued
7. Go online
8. Wait for sync
9. Verify event synced to server

#### ✅ Test 4: Offline Event Editing
1. Start offline (with cached events)
2. Click "Edit" on an event
3. Modify event details
4. Save changes
5. Verify changes reflected in list
6. Go online and wait for sync
7. Verify changes synced to server

#### ✅ Test 5: Offline Event Deletion
1. Start offline
2. Click "Delete" on an event
3. Confirm deletion
4. Verify event removed from list
5. Go online and wait for sync
6. Verify event deleted on server

#### ✅ Test 6: Navigation Between Pages
1. Start offline
2. Navigate to Event List
3. Navigate to Calendar View
4. Navigate back to Event List
5. Verify both pages work offline
6. Verify offline indicator persists across pages

#### ✅ Test 7: Sync After Multiple Operations
1. Start offline
2. Create 2 events
3. Edit 1 event
4. Delete 1 event
5. Verify offline indicator shows pending count
6. Go online
7. Verify "Syncing" indicator appears
8. Wait for sync to complete
9. Verify all operations synced correctly

---

## Troubleshooting

### Events Not Loading Offline

**Symptom**: No events shown when offline

**Possible Causes**:
1. IndexedDB cache is empty (never loaded events while online)
2. Browser cleared IndexedDB storage
3. Different user account (cache is per-user)

**Solution**:
1. Go online
2. Load Event List page
3. Wait for events to load from server
4. Events are now cached for offline use

### Changes Not Syncing

**Symptom**: Offline changes not appearing on server after going online

**Possible Causes**:
1. Sync not triggered automatically
2. Authentication token expired
3. Server unreachable

**Solution**:
1. Check browser console for errors
2. Verify offline indicator shows "Syncing" state
3. Check server logs for sync errors
4. Try manual page refresh

### Offline Indicator Not Showing

**Symptom**: No offline indicator visible

**Possible Causes**:
1. Browser doesn't support navigator.onLine API
2. JavaScript error preventing initialization
3. CSS not loaded

**Solution**:
1. Check browser console for errors
2. Verify `connectivity-manager.js` is loaded
3. Clear browser cache and reload

---

## Best Practices

### For Users

1. **Load Data While Online**
   - Open Event List page while online to cache events
   - This ensures offline functionality works smoothly

2. **Check Sync Status**
   - Watch for the "Syncing" indicator when going online
   - Wait for "Synced" confirmation before closing browser

3. **Minimize Conflicts**
   - Avoid editing same events on multiple devices while offline
   - Last change wins in conflict resolution

### For Developers

1. **Always Use OfflineEventService**
   - Don't call ApiService directly from UI components
   - Let OfflineEventService handle online/offline logic

2. **Subscribe to Connectivity Events**
   - React to connectivity changes in UI
   - Update loading states appropriately

3. **Handle Errors Gracefully**
   - All operations can fail (network, storage, etc.)
   - Show user-friendly error messages
   - Log errors for debugging

4. **Test Offline Scenarios**
   - Test all features offline before deployment
   - Test sync with various operation combinations
   - Test edge cases (interrupted sync, etc.)

---

## Related Documentation

- [Offline Mode Comprehensive Guide](OFFLINE_MODE_COMPREHENSIVE_GUIDE.md) - Complete offline mode documentation
- [Calendar View Offline Summary](CALENDARVIEW_OFFLINE_SUMMARY.md) - Calendar view offline features
- [Offline Mode Troubleshooting](OFFLINE_MODE_TROUBLESHOOTING.md) - Common issues and solutions
- [Offline Mode Quick Reference](OFFLINE_MODE_QUICK_REFERENCE.md) - Quick reference guide

---

**Last Updated**: October 18, 2025  
**Version**: 1.0  
**Author**: Development Team
