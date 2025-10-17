# Offline Support - Developer Guide

## Quick Start

This guide will help you implement offline support in your EventScheduler pages.

## Basic Integration

### 1. Using OfflineEventService

Replace all `ApiService` event calls with `OfflineEventService`:

```razor
@page "/my-events"
@inject OfflineEventService EventService
@inject ConnectivityService ConnectivityService

<h3>My Events @(ConnectivityService.IsOnline ? "" : "(Offline)")</h3>

<button @onclick="LoadEvents">Load Events</button>
<button @onclick="CreateEvent">Create Event</button>

@code {
    private List<EventResponse> events = new();
    
    private async Task LoadEvents()
    {
        // Works online or offline
        events = await EventService.GetEventsAsync();
    }
    
    private async Task CreateEvent()
    {
        var request = new CreateEventRequest
        {
            Title = "New Event",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1)
        };
        
        // Automatically saved to server if online,
        // or queued for sync if offline
        var result = await EventService.CreateEventAsync(request);
        
        if (result != null)
        {
            events.Add(result);
        }
    }
}
```

### 2. Monitoring Connectivity

```razor
@implements IDisposable
@inject ConnectivityService ConnectivityService

<div class="alert @(isOnline ? "alert-success" : "alert-warning")">
    @(isOnline ? "Connected" : "Offline - Changes will sync when you reconnect")
</div>

@code {
    private bool isOnline = true;
    
    protected override async Task OnInitializedAsync()
    {
        await ConnectivityService.InitializeAsync();
        isOnline = ConnectivityService.IsOnline;
        ConnectivityService.ConnectivityChanged += OnConnectivityChanged;
    }
    
    private void OnConnectivityChanged(object? sender, bool online)
    {
        isOnline = online;
        InvokeAsync(StateHasChanged);
    }
    
    public void Dispose()
    {
        ConnectivityService.ConnectivityChanged -= OnConnectivityChanged;
    }
}
```

### 3. Handling Sync Events

```razor
@implements IDisposable
@inject SyncService SyncService

@if (isSyncing)
{
    <div class="spinner-border" role="status">
        <span class="visually-hidden">Syncing...</span>
    </div>
}

@if (syncMessage != null)
{
    <div class="alert alert-info">@syncMessage</div>
}

@code {
    private bool isSyncing = false;
    private string? syncMessage;
    
    protected override void OnInitialized()
    {
        SyncService.SyncStarted += OnSyncStarted;
        SyncService.SyncCompleted += OnSyncCompleted;
    }
    
    private void OnSyncStarted(object? sender, EventArgs e)
    {
        isSyncing = true;
        syncMessage = null;
        InvokeAsync(StateHasChanged);
    }
    
    private void OnSyncCompleted(object? sender, SyncResult result)
    {
        isSyncing = false;
        
        if (result.Success)
        {
            syncMessage = $"Synced {result.SyncedEvents} events";
        }
        else
        {
            syncMessage = $"Sync failed: {result.Message}";
        }
        
        InvokeAsync(StateHasChanged);
    }
    
    public void Dispose()
    {
        SyncService.SyncStarted -= OnSyncStarted;
        SyncService.SyncCompleted -= OnSyncCompleted;
    }
}
```

## Complete Example

Here's a complete example of a page with offline support:

```razor
@page "/events"
@using EventScheduler.Application.DTOs.Request
@using EventScheduler.Application.DTOs.Response
@inject OfflineEventService EventService
@inject ConnectivityService ConnectivityService
@inject SyncService SyncService
@implements IDisposable

<PageTitle>Events</PageTitle>

<div class="container mt-4">
    <div class="row mb-3">
        <div class="col">
            <h1>My Events</h1>
        </div>
        <div class="col-auto">
            @if (!isOnline)
            {
                <span class="badge bg-warning">
                    <i class="bi bi-wifi-off"></i> Offline
                </span>
            }
            @if (pendingCount > 0)
            {
                <span class="badge bg-info">
                    @pendingCount pending sync
                </span>
            }
        </div>
    </div>

    @if (isSyncing)
    {
        <div class="alert alert-info">
            <div class="spinner-border spinner-border-sm me-2"></div>
            Syncing changes...
        </div>
    }

    @if (errorMessage != null)
    {
        <div class="alert alert-danger">
            @errorMessage
        </div>
    }

    <div class="row mb-3">
        <div class="col">
            <button class="btn btn-primary" @onclick="ShowCreateModal">
                <i class="bi bi-plus-circle"></i> New Event
            </button>
            
            @if (!isOnline && pendingCount > 0)
            {
                <button class="btn btn-secondary ms-2" @onclick="ManualSync" disabled>
                    <i class="bi bi-arrow-repeat"></i> Sync (reconnect first)
                </button>
            }
            else if (pendingCount > 0)
            {
                <button class="btn btn-secondary ms-2" @onclick="ManualSync">
                    <i class="bi bi-arrow-repeat"></i> Sync Now
                </button>
            }
        </div>
    </div>

    @if (isLoading)
    {
        <div class="text-center">
            <div class="spinner-border" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    }
    else if (events.Count == 0)
    {
        <div class="alert alert-info">
            No events found. Create your first event!
        </div>
    }
    else
    {
        <div class="row">
            @foreach (var evt in events)
            {
                <div class="col-md-6 mb-3">
                    <div class="card">
                        <div class="card-body">
                            <h5 class="card-title">@evt.Title</h5>
                            <p class="card-text">@evt.Description</p>
                            <p class="text-muted">
                                <i class="bi bi-calendar"></i>
                                @evt.StartDate.ToString("MMM dd, yyyy h:mm tt")
                            </p>
                            
                            @if (evt.Id < 0)
                            {
                                <span class="badge bg-warning">Pending Sync</span>
                            }
                            
                            <div class="mt-2">
                                <button class="btn btn-sm btn-primary" 
                                        @onclick="() => EditEvent(evt.Id)">
                                    Edit
                                </button>
                                <button class="btn btn-sm btn-danger ms-2" 
                                        @onclick="() => DeleteEvent(evt.Id)">
                                    Delete
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            }
        </div>
    }
</div>

@code {
    private List<EventResponse> events = new();
    private bool isLoading = true;
    private bool isOnline = true;
    private bool isSyncing = false;
    private int pendingCount = 0;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        // Initialize connectivity monitoring
        await ConnectivityService.InitializeAsync();
        isOnline = ConnectivityService.IsOnline;
        ConnectivityService.ConnectivityChanged += OnConnectivityChanged;

        // Subscribe to sync events
        SyncService.SyncStarted += OnSyncStarted;
        SyncService.SyncCompleted += OnSyncCompleted;

        // Load events
        await LoadEvents();
        await UpdatePendingCount();
    }

    private async Task LoadEvents()
    {
        try
        {
            isLoading = true;
            errorMessage = null;
            events = await EventService.GetEventsAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load events: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ShowCreateModal()
    {
        // Implement modal logic
    }

    private async Task EditEvent(int id)
    {
        // Implement edit logic
    }

    private async Task DeleteEvent(int id)
    {
        if (await ConfirmDelete())
        {
            try
            {
                await EventService.DeleteEventAsync(id);
                await LoadEvents();
                await UpdatePendingCount();
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to delete event: {ex.Message}";
            }
        }
    }

    private async Task<bool> ConfirmDelete()
    {
        // Implement confirmation dialog
        return true;
    }

    private async Task ManualSync()
    {
        try
        {
            var result = await SyncService.SyncAsync();
            
            if (result.Success)
            {
                await LoadEvents();
                await UpdatePendingCount();
            }
            else
            {
                errorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Sync failed: {ex.Message}";
        }
    }

    private async Task UpdatePendingCount()
    {
        pendingCount = await SyncService.GetPendingOperationsCountAsync();
    }

    private void OnConnectivityChanged(object? sender, bool online)
    {
        isOnline = online;
        InvokeAsync(StateHasChanged);
    }

    private void OnSyncStarted(object? sender, EventArgs e)
    {
        isSyncing = true;
        InvokeAsync(StateHasChanged);
    }

    private void OnSyncCompleted(object? sender, SyncResult result)
    {
        isSyncing = false;
        
        InvokeAsync(async () =>
        {
            await LoadEvents();
            await UpdatePendingCount();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        ConnectivityService.ConnectivityChanged -= OnConnectivityChanged;
        SyncService.SyncStarted -= OnSyncStarted;
        SyncService.SyncCompleted -= OnSyncCompleted;
    }
}
```

## Common Patterns

### Optimistic UI Updates

Update UI immediately, then sync in background:

```csharp
private async Task CreateEvent(CreateEventRequest request)
{
    // Create temporary event for immediate UI update
    var tempEvent = new EventResponse
    {
        Id = -(DateTime.Now.Ticks % int.MaxValue),
        Title = request.Title,
        // ... other fields
    };
    
    events.Add(tempEvent);
    StateHasChanged();
    
    // Create in background
    var result = await EventService.CreateEventAsync(request);
    
    if (result != null)
    {
        // Replace temp with real event
        events.Remove(tempEvent);
        events.Add(result);
        StateHasChanged();
    }
}
```

### Error Handling

```csharp
private async Task SaveEvent()
{
    try
    {
        var result = await EventService.UpdateEventAsync(eventId, request);
        
        if (result != null)
        {
            // Success
        }
    }
    catch (UnauthorizedAccessException)
    {
        // Session expired - redirect to login
        navigationManager.NavigateTo("/login");
    }
    catch (InvalidOperationException ex)
    {
        // Validation error
        errorMessage = ex.Message;
    }
    catch (Exception ex)
    {
        // General error
        errorMessage = "An error occurred. Please try again.";
        logger.LogError(ex, "Failed to save event");
    }
}
```

### Refresh on Sync

Automatically refresh data when sync completes:

```csharp
private void OnSyncCompleted(object? sender, SyncResult result)
{
    if (result.Success && result.SyncedEvents > 0)
    {
        InvokeAsync(async () =>
        {
            await LoadEvents();
            StateHasChanged();
        });
    }
}
```

## Tips

1. **Always dispose**: Unsubscribe from events in `Dispose()` to prevent memory leaks

2. **Use InvokeAsync**: When updating UI from event handlers, use `InvokeAsync(StateHasChanged)`

3. **Check connectivity**: For operations that absolutely require internet, check `ConnectivityService.IsOnline`

4. **Show pending count**: Display pending operations count so users know what will sync

5. **Handle temp IDs**: Events created offline have negative IDs - handle this in your UI

6. **Clear cache on logout**: Call `LocalStorage.ClearAllDataAsync()` when user logs out

## Testing

### Test Offline Create
1. Go offline in browser DevTools
2. Create a new event
3. Verify event shows with "Pending Sync" badge
4. Go back online
5. Verify sync occurs automatically
6. Verify event now has real ID

### Test Offline Update
1. Load events while online
2. Go offline
3. Edit an event
4. Verify changes appear locally
5. Go back online
6. Verify sync updates server

### Test Offline Delete
1. Go offline
2. Delete an event
3. Verify it disappears from list
4. Go back online
5. Verify deletion syncs to server

## Debugging

Enable verbose logging:

```csharp
// In Program.cs
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

Check IndexedDB in browser:
1. Open DevTools
2. Go to Application tab
3. Expand IndexedDB → EventSchedulerDB
4. Inspect events, pendingOperations, syncMetadata

Monitor service worker:
1. Open DevTools
2. Go to Application tab
3. Click Service Workers
4. View console messages

## Migration from ApiService

Replace:
```csharp
@inject ApiService ApiService
var events = await ApiService.GetAllEventsAsync();
```

With:
```csharp
@inject OfflineEventService EventService
var events = await EventService.GetEventsAsync();
```

## Summary

The offline support is designed to be transparent - use `OfflineEventService` instead of `ApiService`, and everything else happens automatically:

- ✅ Works online and offline
- ✅ Automatic sync on reconnection
- ✅ Visual indicators for offline status
- ✅ Conflict resolution built-in
- ✅ Pending operations queue
- ✅ No code changes needed in most cases

For questions or issues, see [OFFLINE_SUPPORT_ARCHITECTURE.md](OFFLINE_SUPPORT_ARCHITECTURE.md).
