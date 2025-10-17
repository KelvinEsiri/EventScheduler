using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using System.Text.Json;

namespace EventScheduler.Web.Services;

public class OfflineSyncService : IDisposable
{
    private readonly ApiService _apiService;
    private readonly OfflineStorageService _offlineStorage;
    private readonly NetworkStatusService _networkStatus;
    private readonly ILogger<OfflineSyncService> _logger;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private bool _isSyncing = false;
    private int _pendingCount = 0;
    private bool _disposed = false;

    public event Func<string, Task>? OnSyncStatusChanged;
    public event Func<int, Task>? OnPendingOperationsCountChanged;
    
    public bool IsSyncing => _isSyncing;
    public int PendingCount => _pendingCount;

    public OfflineSyncService(
        ApiService apiService,
        OfflineStorageService offlineStorage,
        NetworkStatusService networkStatus,
        ILogger<OfflineSyncService> logger)
    {
        _apiService = apiService;
        _offlineStorage = offlineStorage;
        _networkStatus = networkStatus;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _offlineStorage.InitializeDatabaseAsync();
        await _networkStatus.InitializeAsync();
        
        _networkStatus.OnStatusChanged += HandleNetworkStatusChange;
        
        _apiService.SetNetworkStatusProvider(() => _networkStatus.IsOnline);
        _apiService.SetOfflineFallbackHandler(HandleOfflineMode);
    }

    private async Task HandleNetworkStatusChange(bool isOnline)
    {
        if (isOnline && !_isSyncing)
        {
            _logger.LogInformation("Network restored, starting synchronization");
            await SynchronizePendingOperationsAsync();
        }
    }

    private async Task HandleOfflineMode()
    {
        _logger.LogInformation("Offline mode activated, loading cached events");
        var cachedEvents = await _offlineStorage.GetEventsAsync();
        
        if (cachedEvents.Any())
        {
            _logger.LogInformation("Loaded {Count} events from cache", cachedEvents.Count);
        }
    }

    public async Task<List<EventResponse>> LoadEventsAsync()
    {
        if (_networkStatus.IsOnline)
        {
            try
            {
                var events = await _apiService.GetAllEventsAsync();
                await _offlineStorage.SaveEventsAsync(events);
                _logger.LogInformation("Events loaded from API and cached");
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load events from API, falling back to cache");
                return await _offlineStorage.GetEventsAsync();
            }
        }
        else
        {
            _logger.LogInformation("Offline mode, loading from cache");
            return await _offlineStorage.GetEventsAsync();
        }
    }

    public async Task<EventResponse?> CreateEventOfflineAsync(CreateEventRequest request)
    {
        var tempEvent = new EventResponse
        {
            Id = -DateTime.UtcNow.Ticks.GetHashCode(),
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            IsAllDay = request.IsAllDay,
            Color = request.Color,
            EventType = request.EventType,
            IsPublic = request.IsPublic,
            Status = "Scheduled"
        };

        var operation = new PendingOperation
        {
            Type = "create",
            EventData = JsonSerializer.Serialize(request),
            Timestamp = DateTime.UtcNow
        };

        await _offlineStorage.AddPendingOperationAsync(operation);
        
        var events = await _offlineStorage.GetEventsAsync();
        events.Add(tempEvent);
        await _offlineStorage.SaveEventsAsync(events);

        await NotifyPendingOperationsCount();
        
        _logger.LogInformation("Event queued for creation offline with temp ID: {TempId}", tempEvent.Id);
        return tempEvent;
    }

    public async Task UpdateEventOfflineAsync(int eventId, UpdateEventRequest request)
    {
        var operation = new PendingOperation
        {
            Type = "update",
            EventId = eventId,
            EventData = JsonSerializer.Serialize(request),
            Timestamp = DateTime.UtcNow
        };

        await _offlineStorage.AddPendingOperationAsync(operation);
        
        var events = await _offlineStorage.GetEventsAsync();
        var existingEvent = events.FirstOrDefault(e => e.Id == eventId);
        if (existingEvent != null)
        {
            existingEvent.Title = request.Title;
            existingEvent.Description = request.Description;
            existingEvent.StartDate = request.StartDate;
            existingEvent.EndDate = request.EndDate;
            existingEvent.Location = request.Location;
            existingEvent.IsAllDay = request.IsAllDay;
            existingEvent.Color = request.Color;
            existingEvent.EventType = request.EventType;
            existingEvent.IsPublic = request.IsPublic;
            existingEvent.Status = request.Status ?? existingEvent.Status;
            
            await _offlineStorage.SaveEventsAsync(events);
        }

        await NotifyPendingOperationsCount();
        
        _logger.LogInformation("Event {EventId} queued for update offline", eventId);
    }

    public async Task DeleteEventOfflineAsync(int eventId)
    {
        var operation = new PendingOperation
        {
            Type = "delete",
            EventId = eventId,
            Timestamp = DateTime.UtcNow
        };

        await _offlineStorage.AddPendingOperationAsync(operation);
        
        var events = await _offlineStorage.GetEventsAsync();
        var eventToRemove = events.FirstOrDefault(e => e.Id == eventId);
        if (eventToRemove != null)
        {
            events.Remove(eventToRemove);
            await _offlineStorage.SaveEventsAsync(events);
        }

        await NotifyPendingOperationsCount();
        
        _logger.LogInformation("Event {EventId} queued for deletion offline", eventId);
    }

    public async Task SynchronizePendingOperationsAsync()
    {
        if (!await _syncLock.WaitAsync(0))
        {
            _logger.LogInformation("Sync already in progress, skipping duplicate request");
            return;
        }

        try
        {
            if (!_networkStatus.IsOnline)
            {
                _logger.LogInformation("Cannot sync - offline");
                return;
            }

            _isSyncing = true;
            await InvokeAsync(() => NotifySyncStatus("Synchronizing pending changes..."));
            
            var pendingOperations = await _offlineStorage.GetPendingOperationsAsync();
            
            if (pendingOperations.Count == 0)
            {
                _logger.LogInformation("No pending operations to sync");
                await InvokeAsync(() => NotifyPendingOperationsCount());
                return;
            }
            
            _logger.LogInformation("Found {Count} pending operations to sync", pendingOperations.Count);

            var successCount = 0;
            var failureCount = 0;

            foreach (var operation in pendingOperations.OrderBy(o => o.Timestamp))
            {
                try
                {
                    await ProcessPendingOperationAsync(operation);
                    await _offlineStorage.RemovePendingOperationAsync(operation.Id);
                    successCount++;
                    _logger.LogInformation("Synced operation {OperationId}: {Type}", operation.Id, operation.Type);
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex, "Failed to sync operation {OperationId}: {Type}. Operation will be retried on next sync.", operation.Id, operation.Type);
                }
            }

            if (failureCount == 0)
            {
                var freshEvents = await _apiService.GetAllEventsAsync();
                await _offlineStorage.SaveEventsAsync(freshEvents);
                await InvokeAsync(() => NotifySyncStatus("Synchronization complete"));
                _logger.LogInformation("Synchronization completed: {Success} operations synced successfully", successCount);
            }
            else
            {
                await InvokeAsync(() => NotifySyncStatus($"Sync partial: {successCount} succeeded, {failureCount} failed"));
                _logger.LogWarning("Synchronization partially completed: {Success} succeeded, {Failures} failed", successCount, failureCount);
            }
            
            await InvokeAsync(() => NotifyPendingOperationsCount());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during synchronization");
            await InvokeAsync(() => NotifySyncStatus("Synchronization failed"));
        }
        finally
        {
            _isSyncing = false;
            _syncLock.Release();
        }
    }
    
    private async Task InvokeAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invoke notification handler");
        }
    }

    private async Task ProcessPendingOperationAsync(PendingOperation operation)
    {
        switch (operation.Type.ToLower())
        {
            case "create":
                if (string.IsNullOrEmpty(operation.EventData))
                {
                    _logger.LogWarning("Create operation {OpId} missing event data", operation.Id);
                    return;
                }
                
                var createRequest = JsonSerializer.Deserialize<CreateEventRequest>(operation.EventData);
                if (createRequest == null)
                {
                    _logger.LogWarning("Create operation {OpId} has invalid event data", operation.Id);
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(createRequest.Title))
                {
                    _logger.LogWarning("Create operation {OpId} missing required Title field", operation.Id);
                    return;
                }
                
                _logger.LogInformation("Syncing create operation: {Title}", createRequest.Title);
                await _apiService.CreateEventAsync(createRequest);
                break;

            case "update":
                if (!operation.EventId.HasValue)
                {
                    _logger.LogWarning("Update operation {OpId} missing EventId", operation.Id);
                    return;
                }
                
                if (string.IsNullOrEmpty(operation.EventData))
                {
                    _logger.LogWarning("Update operation {OpId} missing event data", operation.Id);
                    return;
                }
                
                try
                {
                    var updateRequest = JsonSerializer.Deserialize<UpdateEventRequest>(
                        operation.EventData, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    
                    if (updateRequest == null)
                    {
                        _logger.LogWarning("Update operation {OpId} has invalid event data", operation.Id);
                        return;
                    }
                    
                    if (string.IsNullOrWhiteSpace(updateRequest.Title))
                    {
                        _logger.LogWarning("Update operation {OpId} missing required Title field", operation.Id);
                        return;
                    }
                    
                    _logger.LogInformation("Syncing update for event {EventId}: {Title}", operation.EventId.Value, updateRequest.Title);
                    await _apiService.UpdateEventAsync(operation.EventId.Value, updateRequest);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize update operation {OpId} for event {EventId}", operation.Id, operation.EventId.Value);
                    throw;
                }
                break;

            case "delete":
                if (!operation.EventId.HasValue)
                {
                    _logger.LogWarning("Delete operation {OpId} missing EventId", operation.Id);
                    return;
                }
                
                _logger.LogInformation("Syncing delete for event {EventId}", operation.EventId.Value);
                await _apiService.DeleteEventAsync(operation.EventId.Value);
                break;

            default:
                _logger.LogWarning("Unknown operation type: {Type}", operation.Type);
                break;
        }
    }

    private async Task NotifySyncStatus(string status)
    {
        if (OnSyncStatusChanged != null)
        {
            await OnSyncStatusChanged.Invoke(status);
        }
    }

    private async Task NotifyPendingOperationsCount()
    {
        var pendingOperations = await _offlineStorage.GetPendingOperationsAsync();
        _pendingCount = pendingOperations.Count;
        if (OnPendingOperationsCountChanged != null)
        {
            await OnPendingOperationsCountChanged.Invoke(_pendingCount);
        }
    }

    public async Task<int> GetPendingOperationsCountAsync()
    {
        var pendingOperations = await _offlineStorage.GetPendingOperationsAsync();
        _pendingCount = pendingOperations.Count;
        return _pendingCount;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _syncLock?.Dispose();
            _disposed = true;
        }
    }
}
