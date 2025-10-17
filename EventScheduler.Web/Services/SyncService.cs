using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;

namespace EventScheduler.Web.Services;

/// <summary>
/// Service for synchronizing local and server data
/// Handles conflict resolution and offline operation queuing
/// </summary>
public class SyncService
{
    private readonly LocalStorageService _localStorage;
    private readonly ApiService _apiService;
    private readonly ConnectivityService _connectivityService;
    private readonly ILogger<SyncService> _logger;
    private bool _isSyncing = false;

    public event EventHandler? SyncStarted;
    public event EventHandler<SyncResult>? SyncCompleted;

    public SyncService(
        LocalStorageService localStorage,
        ApiService apiService,
        ConnectivityService connectivityService,
        ILogger<SyncService> logger)
    {
        _localStorage = localStorage;
        _apiService = apiService;
        _connectivityService = connectivityService;
        _logger = logger;
    }

    /// <summary>
    /// Perform full synchronization
    /// </summary>
    public async Task<SyncResult> SyncAsync()
    {
        if (_isSyncing)
        {
            _logger.LogWarning("Sync already in progress");
            return new SyncResult { Success = false, Message = "Sync already in progress" };
        }

        if (!_connectivityService.IsOnline)
        {
            _logger.LogWarning("Cannot sync while offline");
            return new SyncResult { Success = false, Message = "Cannot sync while offline" };
        }

        _isSyncing = true;
        SyncStarted?.Invoke(this, EventArgs.Empty);

        var result = new SyncResult { Success = true };

        try
        {
            _logger.LogInformation("Starting sync...");

            // Step 1: Process pending operations
            var pendingOps = await _localStorage.GetPendingOperationsAsync();
            _logger.LogInformation("Found {Count} pending operations", pendingOps.Count);

            foreach (var operation in pendingOps)
            {
                try
                {
                    await ProcessPendingOperationAsync(operation);
                    await _localStorage.DeletePendingOperationAsync(operation.Id);
                    result.ProcessedOperations++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process pending operation: {OperationId}", operation.Id);
                    result.FailedOperations++;
                }
            }

            // Step 2: Pull latest events from server
            var serverEvents = await _apiService.GetAllEventsAsync();
            _logger.LogInformation("Retrieved {Count} events from server", serverEvents?.Count ?? 0);

            if (serverEvents != null)
            {
                // Step 3: Merge with local events
                var localEvents = await _localStorage.GetAllEventsAsync();
                var mergedEvents = MergeEvents(localEvents, serverEvents);

                // Step 4: Update local storage with merged data
                foreach (var evt in mergedEvents)
                {
                    await _localStorage.SaveEventAsync(evt);
                }

                result.SyncedEvents = mergedEvents.Count;
            }

            // Step 5: Update last sync timestamp
            await _localStorage.SaveSyncMetadataAsync("lastSyncTime", DateTime.UtcNow.ToString("o"));

            result.Success = true;
            result.Message = "Sync completed successfully";
            _logger.LogInformation("Sync completed: {Processed} processed, {Failed} failed, {Synced} synced",
                result.ProcessedOperations, result.FailedOperations, result.SyncedEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
            result.Success = false;
            result.Message = $"Sync failed: {ex.Message}";
        }
        finally
        {
            _isSyncing = false;
            SyncCompleted?.Invoke(this, result);
        }

        return result;
    }

    /// <summary>
    /// Process a single pending operation
    /// </summary>
    private async Task ProcessPendingOperationAsync(PendingOperation operation)
    {
        _logger.LogInformation("Processing pending operation: {Type} {Endpoint}", 
            operation.Type, operation.Endpoint);

        switch (operation.Type.ToUpper())
        {
            case "POST":
                if (operation.Data is CreateEventRequest createRequest)
                {
                    await _apiService.CreateEventAsync(createRequest);
                }
                break;

            case "PUT":
                if (operation.Data is UpdateEventRequest updateRequest && 
                    operation.Endpoint.Contains("/api/events/"))
                {
                    var eventId = ExtractEventIdFromEndpoint(operation.Endpoint);
                    if (eventId > 0)
                    {
                        await _apiService.UpdateEventAsync(eventId, updateRequest);
                    }
                }
                break;

            case "DELETE":
                var deleteEventId = ExtractEventIdFromEndpoint(operation.Endpoint);
                if (deleteEventId > 0)
                {
                    await _apiService.DeleteEventAsync(deleteEventId);
                }
                break;

            default:
                _logger.LogWarning("Unknown operation type: {Type}", operation.Type);
                break;
        }
    }

    /// <summary>
    /// Extract event ID from endpoint URL
    /// </summary>
    private int ExtractEventIdFromEndpoint(string endpoint)
    {
        try
        {
            var parts = endpoint.Split('/');
            var lastPart = parts[^1];
            return int.Parse(lastPart);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Merge local and server events with conflict resolution
    /// Strategy: Last-write-wins based on UpdatedAt timestamp
    /// </summary>
    private List<EventResponse> MergeEvents(
        List<EventResponse> localEvents, 
        List<EventResponse> serverEvents)
    {
        var merged = new Dictionary<int, EventResponse>();

        // Add all server events (they are the source of truth)
        foreach (var serverEvent in serverEvents)
        {
            merged[serverEvent.Id] = serverEvent;
        }

        // Merge local events, keeping local if newer
        foreach (var localEvent in localEvents)
        {
            if (!merged.ContainsKey(localEvent.Id))
            {
                // Local event not on server (might be pending sync)
                merged[localEvent.Id] = localEvent;
            }
            else
            {
                // Conflict: compare timestamps
                var serverEvent = merged[localEvent.Id];
                var localUpdated = localEvent.UpdatedAt ?? localEvent.CreatedAt;
                var serverUpdated = serverEvent.UpdatedAt ?? serverEvent.CreatedAt;

                if (localUpdated > serverUpdated)
                {
                    // Local is newer, keep it
                    _logger.LogInformation("Keeping local version of event {EventId} (local newer)", 
                        localEvent.Id);
                    merged[localEvent.Id] = localEvent;
                }
                else
                {
                    // Server is newer or same, already in merged
                    _logger.LogInformation("Keeping server version of event {EventId} (server newer)", 
                        serverEvent.Id);
                }
            }
        }

        return merged.Values.ToList();
    }

    /// <summary>
    /// Queue an operation for later synchronization
    /// </summary>
    public async Task QueueOperationAsync(string type, string endpoint, object? data, string? token)
    {
        var operation = new PendingOperation
        {
            Type = type,
            Endpoint = endpoint,
            Data = data,
            Token = token,
            Timestamp = DateTime.UtcNow
        };

        await _localStorage.SavePendingOperationAsync(operation);
        _logger.LogInformation("Operation queued: {Type} {Endpoint}", type, endpoint);
    }

    /// <summary>
    /// Get pending operations count
    /// </summary>
    public async Task<int> GetPendingOperationsCountAsync()
    {
        var operations = await _localStorage.GetPendingOperationsAsync();
        return operations.Count;
    }

    /// <summary>
    /// Check if sync is currently in progress
    /// </summary>
    public bool IsSyncing => _isSyncing;
}

/// <summary>
/// Result of a sync operation
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProcessedOperations { get; set; }
    public int FailedOperations { get; set; }
    public int SyncedEvents { get; set; }
}
