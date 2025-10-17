# Offline Support Architecture

## Overview

EventScheduler now includes comprehensive offline support, allowing users to continue using all calendar functionalities even when disconnected from the internet. All changes made offline are automatically synchronized when the connection is restored.

## Architecture Design

### 1. **Progressive Web App (PWA)**
- Service Worker for caching and background sync
- Web App Manifest for installability
- Offline fallback page

### 2. **Client-Side Storage**
- IndexedDB for persistent event storage
- Pending operations queue
- Sync metadata storage

### 3. **Connectivity Detection**
- Real-time online/offline status monitoring
- Automatic sync trigger on reconnection
- Visual offline indicator

### 4. **Sync Engine**
- Last-write-wins conflict resolution
- Pending operations processing
- Server-client data merging

## Components

### Service Layer

#### 1. ConnectivityService
**Location:** `EventScheduler.Web/Services/ConnectivityService.cs`

Monitors network connectivity and triggers sync events.

**Key Features:**
- Detects online/offline transitions
- Periodic connectivity checks
- Automatic sync on reconnection

**Usage:**
```csharp
@inject ConnectivityService ConnectivityService

protected override async Task OnInitializedAsync()
{
    await ConnectivityService.InitializeAsync();
    ConnectivityService.ConnectivityChanged += OnConnectivityChanged;
    
    if (ConnectivityService.IsOnline)
    {
        // Online logic
    }
}
```

#### 2. LocalStorageService
**Location:** `EventScheduler.Web/Services/LocalStorageService.cs`

Manages IndexedDB for persistent client-side storage.

**Key Features:**
- Event CRUD operations in IndexedDB
- Pending operations management
- Sync metadata storage

**Usage:**
```csharp
@inject LocalStorageService LocalStorage

// Save event locally
await LocalStorage.SaveEventAsync(eventResponse);

// Get all local events
var events = await LocalStorage.GetAllEventsAsync(userId);

// Queue a pending operation
await LocalStorage.SavePendingOperationAsync(new PendingOperation
{
    Type = "POST",
    Endpoint = "/api/events",
    Data = createRequest,
    Token = token
});
```

#### 3. SyncService
**Location:** `EventScheduler.Web/Services/SyncService.cs`

Synchronizes local and server data with conflict resolution.

**Key Features:**
- Process pending operations
- Merge local and server events
- Timestamp-based conflict resolution

**Usage:**
```csharp
@inject SyncService SyncService

// Perform sync
var result = await SyncService.SyncAsync();

if (result.Success)
{
    Console.WriteLine($"Synced {result.SyncedEvents} events");
    Console.WriteLine($"Processed {result.ProcessedOperations} operations");
}

// Subscribe to sync events
SyncService.SyncStarted += OnSyncStarted;
SyncService.SyncCompleted += OnSyncCompleted;
```

#### 4. OfflineEventService
**Location:** `EventScheduler.Web/Services/OfflineEventService.cs`

Offline-first event service with automatic fallback.

**Key Features:**
- Network-first with local cache fallback
- Automatic operation queuing when offline
- Seamless online/offline transitions

**Usage:**
```csharp
@inject OfflineEventService EventService

// Create event (works online or offline)
var result = await EventService.CreateEventAsync(request);

// Get events (from server if online, local cache if offline)
var events = await EventService.GetEventsAsync();

// Update event
var updated = await EventService.UpdateEventAsync(eventId, request);

// Delete event
await EventService.DeleteEventAsync(eventId);
```

### JavaScript Components

#### 1. Service Worker
**Location:** `wwwroot/service-worker.js`

Handles caching and background sync.

**Features:**
- Network-first strategy for API calls
- Cache-first strategy for static resources
- Background sync registration

#### 2. IndexedDB Manager
**Location:** `wwwroot/js/indexeddb-manager.js`

Client-side database management.

**Features:**
- Event storage with sync metadata
- Pending operations queue
- Sync metadata management

#### 3. Connectivity Manager
**Location:** `wwwroot/js/connectivity-manager.js`

Monitors connectivity and triggers sync.

**Features:**
- Online/offline event listeners
- Periodic connectivity checks
- Automatic sync triggering

### UI Components

#### OfflineIndicator
**Location:** `EventScheduler.Web/Components/OfflineIndicator.razor`

Visual indicator showing connectivity status.

**Features:**
- Shows "Offline Mode" when disconnected
- Displays pending operations count
- Shows sync progress
- Auto-hides after successful sync

## Data Flow

### Online Mode
```
User Action → OfflineEventService → ApiService → Server
                     ↓
              LocalStorageService (cache)
```

### Offline Mode
```
User Action → OfflineEventService → LocalStorageService
                     ↓
              SyncService (queue operation)
```

### Sync Process
```
Connection Restored → ConnectivityService
                           ↓
                      SyncService
                           ↓
        ┌─────────────────┴──────────────────┐
        ↓                                     ↓
Process Pending Operations          Pull Server Events
        ↓                                     ↓
    ApiService                      Merge with Local
        ↓                                     ↓
Update Local Cache ←────────────────────────┘
```

## Conflict Resolution

The system uses **last-write-wins** strategy based on `UpdatedAt` timestamps:

1. Compare local and server versions of the same event
2. Check `UpdatedAt` timestamp (or `CreatedAt` if `UpdatedAt` is null)
3. Keep the version with the most recent timestamp
4. Log conflict resolution decisions

```csharp
var localUpdated = localEvent.UpdatedAt ?? localEvent.CreatedAt;
var serverUpdated = serverEvent.UpdatedAt ?? serverEvent.CreatedAt;

if (localUpdated > serverUpdated)
{
    // Keep local version
}
else
{
    // Keep server version
}
```

## Offline Operations

### Temporary IDs
Events created offline receive negative IDs to distinguish them from server events:

```csharp
var tempId = -(int)(DateTime.UtcNow.Ticks % int.MaxValue);
```

When synced, the server returns the real ID, and the local event is updated.

### Pending Operations Queue
All operations performed offline are queued:

```csharp
{
    "Type": "POST",          // HTTP method
    "Endpoint": "/api/events",
    "Data": { ... },         // Request payload
    "Token": "jwt-token",    // Auth token
    "Timestamp": "2025-10-17T23:00:00Z"
}
```

### Sync Priority
Operations are processed in timestamp order (FIFO):
1. Oldest operation first
2. If operation fails, it remains in queue
3. Failed operations retry on next sync

## Service Registration

Add to `Program.cs`:

```csharp
builder.Services.AddScoped<ConnectivityService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<OfflineEventService>();
```

## IndexedDB Schema

### events
```
{
    id: number (key),
    title: string,
    description: string,
    startDate: string (ISO 8601),
    endDate: string (ISO 8601),
    userId: number,
    lastModified: string (ISO 8601),
    syncStatus: "pending" | "synced",
    ...other event fields
}
```

### pendingOperations
```
{
    id: number (auto-increment key),
    type: string ("POST" | "PUT" | "DELETE"),
    endpoint: string,
    data: object,
    token: string,
    timestamp: string (ISO 8601)
}
```

### syncMetadata
```
{
    key: string (key),
    value: any,
    timestamp: string (ISO 8601)
}
```

## Testing Offline Mode

### Browser DevTools
1. Open DevTools (F12)
2. Go to Network tab
3. Select "Offline" from throttling dropdown
4. Test creating/editing/deleting events
5. Switch back to "Online"
6. Observe automatic sync

### Service Worker
1. Open Application tab in DevTools
2. Go to Service Workers section
3. Check "Offline" checkbox
4. Test functionality
5. Uncheck to go back online

## Best Practices

### 1. Always Use OfflineEventService
Instead of calling ApiService directly, use OfflineEventService:
```csharp
// ✅ Good
@inject OfflineEventService EventService
var events = await EventService.GetEventsAsync();

// ❌ Avoid
@inject ApiService ApiService
var events = await ApiService.GetAllEventsAsync();
```

### 2. Handle Sync Events
Subscribe to sync events for user feedback:
```csharp
SyncService.SyncStarted += (s, e) => {
    // Show loading indicator
};

SyncService.SyncCompleted += (s, result) => {
    if (result.Success)
    {
        // Show success message
        // Refresh UI if needed
    }
};
```

### 3. Check Connectivity
Before operations that require connection:
```csharp
if (!ConnectivityService.IsOnline)
{
    // Show offline message
    // Queue operation for later
}
```

### 4. Clear Cache on Logout
```csharp
await LocalStorage.ClearAllDataAsync();
```

## Limitations

1. **Blazor Server Dependency**: Blazor Server requires a SignalR connection. True offline mode requires the page to be loaded before going offline.

2. **Static Resources**: Some static resources may not be available offline unless previously cached.

3. **Large Data Sets**: IndexedDB has browser-specific limits (typically 50MB-1GB).

4. **Conflict Resolution**: Last-write-wins may not be suitable for all scenarios. Consider implementing custom resolution for critical data.

## Future Enhancements

1. **Custom Conflict Resolution**: Allow users to choose which version to keep
2. **Incremental Sync**: Only sync changed events since last sync
3. **Optimistic UI Updates**: Show changes immediately before server confirmation
4. **Offline Analytics**: Track offline usage patterns
5. **Background Sync API**: Use native Background Sync for better reliability

## Troubleshooting

### Events Not Syncing
1. Check browser console for errors
2. Verify service worker is active
3. Check pending operations: `await LocalStorage.GetPendingOperationsAsync()`
4. Manually trigger sync: `await SyncService.SyncAsync()`

### Service Worker Not Registering
1. Ensure HTTPS (or localhost)
2. Check service-worker.js path
3. Verify no console errors
4. Clear browser cache and reload

### IndexedDB Errors
1. Check browser compatibility
2. Ensure sufficient storage quota
3. Clear IndexedDB: `await LocalStorage.ClearAllDataAsync()`
4. Check for corrupted data

## References

- [Service Workers API](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API)
- [IndexedDB API](https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API)
- [Background Sync API](https://developer.mozilla.org/en-US/docs/Web/API/Background_Synchronization_API)
- [PWA Documentation](https://web.dev/progressive-web-apps/)
