# Offline Functionality Guide

## Overview

EventScheduler now supports offline functionality, allowing users to continue working with events even when their internet connection is lost. Changes made while offline are automatically synchronized when the connection is restored.

## Features

### 1. **Automatic Network Detection**
- Real-time monitoring of network status using browser APIs
- Visual indicator showing online/offline status
- Automatic detection when connection is restored

### 2. **Offline Event Storage**
- Events are cached locally using IndexedDB
- Last known state is always available offline
- No data loss when connection drops

### 3. **Pending Operations Queue**
- Changes made offline are queued for synchronization
- Operations are persisted across page reloads
- Automatic sync when connection is restored

### 4. **Supported Offline Operations**
- ✅ View cached events
- ✅ Create new events (queued for sync)
- ✅ Update existing events (queued for sync)
- ✅ Delete events (queued for sync)

## Architecture

### Core Components

#### 1. OfflineStorageService
Manages IndexedDB operations for storing events and pending changes.

```csharp
// Initialize database
await offlineStorage.InitializeDatabaseAsync();

// Save events to cache
await offlineStorage.SaveEventsAsync(events);

// Retrieve cached events
var cachedEvents = await offlineStorage.GetEventsAsync();

// Queue pending operation
await offlineStorage.AddPendingOperationAsync(operation);
```

#### 2. NetworkStatusService
Monitors network connectivity and notifies the application of status changes.

```csharp
// Initialize network monitoring
await networkStatus.InitializeAsync();

// Subscribe to status changes
networkStatus.OnStatusChanged += async (isOnline) => {
    // Handle network status change
};

// Check current status
bool isOnline = networkStatus.IsOnline;
```

#### 3. OfflineSyncService
Orchestrates offline mode and handles synchronization.

```csharp
// Initialize sync service
await syncService.InitializeAsync();

// Load events (online or offline)
var events = await syncService.LoadEventsAsync();

// Create event offline
await syncService.CreateEventOfflineAsync(request);

// Synchronize pending operations
await syncService.SynchronizePendingOperationsAsync();
```

### Data Flow

#### Online Mode
```
User Action → ApiService → Backend API → Response → Cache → UI Update
```

#### Offline Mode
```
User Action → OfflineSyncService → IndexedDB → Pending Operations Queue → UI Update
```

#### Reconnection
```
Network Restored → Detect Status Change → Process Pending Operations → Sync with API → Update Cache → Refresh UI
```

## IndexedDB Schema

### Events Store
```javascript
{
  keyPath: 'id',
  data: EventResponse[]
}
```

### Pending Operations Store
```javascript
{
  keyPath: 'Id',
  indexes: ['timestamp'],
  data: {
    Id: string (GUID),
    Type: 'create' | 'update' | 'delete',
    EventId: number (optional),
    EventData: JSON string,
    Timestamp: DateTime
  }
}
```

## Implementation Details

### JavaScript Modules

#### offline-storage.js
Provides IndexedDB wrapper functions:
- `initDB()` - Initialize database with object stores
- `saveEvents()` - Store events array
- `getEvents()` - Retrieve cached events
- `addPendingOperation()` - Queue operation for sync
- `getPendingOperations()` - Get all pending operations
- `removePendingOperation()` - Remove synced operation
- `clearAll()` - Clear all offline data

#### network-status.js
Monitors network connectivity:
- `initialize()` - Set up event listeners
- `isOnline()` - Check current status
- Event listeners for `online` and `offline` browser events

### C# Services

#### ApiService Integration
Enhanced with offline support:
```csharp
// Set network status provider
apiService.SetNetworkStatusProvider(() => networkStatus.IsOnline);

// Set offline fallback handler
apiService.SetOfflineFallbackHandler(async () => {
    // Load from cache when API call fails
});
```

## Usage Example

### In a Blazor Component

```csharp
@inject OfflineSyncService SyncService
@inject NetworkStatusService NetworkStatus

@code {
    private bool isOnline = true;
    private int pendingCount = 0;
    
    protected override async Task OnInitializedAsync()
    {
        await SyncService.InitializeAsync();
        
        // Subscribe to network status
        NetworkStatus.OnStatusChanged += OnNetworkStatusChanged;
        
        // Subscribe to sync notifications
        SyncService.OnPendingOperationsCountChanged += OnPendingCountChanged;
        
        // Load events (works online or offline)
        var events = await SyncService.LoadEventsAsync();
    }
    
    private async Task OnNetworkStatusChanged(bool online)
    {
        isOnline = online;
        StateHasChanged();
    }
    
    private async Task OnPendingCountChanged(int count)
    {
        pendingCount = count;
        StateHasChanged();
    }
    
    private async Task CreateEvent(CreateEventRequest request)
    {
        if (isOnline)
        {
            await ApiService.CreateEventAsync(request);
        }
        else
        {
            await SyncService.CreateEventOfflineAsync(request);
        }
    }
}
```

## User Experience

### Visual Indicators

1. **Network Status Icon**
   - Shows wifi icon when online
   - Shows wifi-off icon when offline
   - Color changes to indicate status

2. **Pending Operations Badge**
   - Displays count of pending operations
   - Visible when changes are queued for sync
   - Clears when synchronized

3. **Sync Status Messages**
   - "Working offline" when disconnected
   - "Synchronizing..." during sync
   - "All changes synchronized" when complete

### Conflict Resolution

When synchronizing offline changes:
1. Operations are processed in timestamp order
2. Creates are processed first
3. Updates are applied to existing events
4. Deletes are processed last
5. After all operations, fresh data is fetched from API

## Best Practices

### 1. Always Initialize Services
```csharp
protected override async Task OnInitializedAsync()
{
    await SyncService.InitializeAsync();
}
```

### 2. Handle Network Status Changes
```csharp
NetworkStatus.OnStatusChanged += async (isOnline) => {
    if (isOnline)
    {
        await SyncService.SynchronizePendingOperationsAsync();
    }
};
```

### 3. Provide User Feedback
- Show network status prominently
- Display pending operations count
- Notify users when sync completes
- Handle errors gracefully

### 4. Test Offline Scenarios
- Test with Chrome DevTools offline mode
- Verify data persists across page reloads
- Ensure sync works correctly
- Test conflict scenarios

## Browser Compatibility

- Chrome/Edge: ✅ Full support
- Firefox: ✅ Full support
- Safari: ✅ Full support
- Mobile browsers: ✅ Full support

IndexedDB is supported in all modern browsers.

## Limitations

1. **No Real-time Updates When Offline**
   - SignalR requires active connection
   - Updates from other users won't appear until online

2. **Storage Limits**
   - IndexedDB has browser-specific limits (typically 50MB+)
   - Rarely an issue for event data

3. **Conflict Resolution**
   - Last write wins for updates
   - No merge logic for concurrent edits

## Security Considerations

1. **Data Storage**
   - Events are stored locally in browser
   - Use HTTPS in production
   - Clear cache on logout

2. **Authentication**
   - Tokens are stored in localStorage
   - Expired tokens require re-authentication
   - Sync fails if token invalid

## Troubleshooting

### Events Not Loading Offline
- Check browser console for IndexedDB errors
- Verify database initialization
- Try clearing browser cache and reload

### Sync Not Working
- Check network status indicator
- Verify pending operations count
- Check browser console for sync errors
- Ensure valid authentication token

### Pending Operations Stuck
- Open browser DevTools
- Check IndexedDB viewer
- Manually clear pending operations if needed:
  ```javascript
  await offlineStorage.clearAll();
  ```

## Future Enhancements

Potential improvements for offline functionality:
- [ ] Conflict resolution UI for concurrent edits
- [ ] Selective sync (choose which operations to sync)
- [ ] Background sync using Service Workers
- [ ] Offline-first architecture
- [ ] Data compression for storage efficiency

## Related Documentation

- [Network Status API](https://developer.mozilla.org/en-US/docs/Web/API/Navigator/onLine)
- [IndexedDB](https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API)
- [Service Workers](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API)
