using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;

namespace EventScheduler.Web.Services;

/// <summary>
/// Offline-first event service
/// Handles event CRUD operations with automatic fallback to local storage when offline
/// </summary>
public class OfflineEventService
{
    private readonly ApiService _apiService;
    private readonly LocalStorageService _localStorage;
    private readonly ConnectivityService _connectivityService;
    private readonly SyncService _syncService;
    private readonly AuthStateProvider _authStateProvider;
    private readonly ILogger<OfflineEventService> _logger;

    public OfflineEventService(
        ApiService apiService,
        LocalStorageService localStorage,
        ConnectivityService connectivityService,
        SyncService syncService,
        AuthStateProvider authStateProvider,
        ILogger<OfflineEventService> logger)
    {
        _apiService = apiService;
        _localStorage = localStorage;
        _connectivityService = connectivityService;
        _syncService = syncService;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all events (offline-first)
    /// </summary>
    public async Task<List<EventResponse>> GetEventsAsync()
    {
        try
        {
            if (_connectivityService.IsOnline)
            {
                // Try to get from server
                var serverEvents = await _apiService.GetAllEventsAsync();
                
                if (serverEvents != null)
                {
                    // Cache events locally
                    foreach (var evt in serverEvents)
                    {
                        await _localStorage.SaveEventAsync(evt);
                    }
                    
                    _logger.LogInformation("Retrieved {Count} events from server", serverEvents.Count);
                    return serverEvents;
                }
            }

            // Fallback to local storage
            var localEvents = await _localStorage.GetAllEventsAsync();
            _logger.LogInformation("Retrieved {Count} events from local storage", localEvents.Count);
            return localEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events, falling back to local storage");
            return await _localStorage.GetAllEventsAsync();
        }
    }

    /// <summary>
    /// Get a single event by ID (offline-first)
    /// </summary>
    public async Task<EventResponse?> GetEventAsync(int eventId)
    {
        try
        {
            if (_connectivityService.IsOnline)
            {
                // Try to get from server
                var serverEvent = await _apiService.GetEventByIdAsync(eventId);
                
                if (serverEvent != null)
                {
                    // Cache event locally
                    await _localStorage.SaveEventAsync(serverEvent);
                    return serverEvent;
                }
            }

            // Fallback to local storage
            return await _localStorage.GetEventAsync(eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get event {EventId}, falling back to local storage", eventId);
            return await _localStorage.GetEventAsync(eventId);
        }
    }

    /// <summary>
    /// Create a new event (offline-first)
    /// </summary>
    public async Task<EventResponse?> CreateEventAsync(CreateEventRequest request)
    {
        try
        {
            if (_connectivityService.IsOnline)
            {
                // Try to create on server
                var serverEvent = await _apiService.CreateEventAsync(request);
                
                if (serverEvent != null)
                {
                    // Cache event locally
                    await _localStorage.SaveEventAsync(serverEvent);
                    _logger.LogInformation("Event created on server: {EventId}", serverEvent.Id);
                    return serverEvent;
                }
            }

            // Offline: create temporary local event and queue operation
            var tempEvent = CreateTemporaryEvent(request);
            await _localStorage.SaveEventAsync(tempEvent);
            
            // Queue the operation for later sync
            var token = _authStateProvider.GetToken();
            await _syncService.QueueOperationAsync("POST", "/api/events", request, token);
            
            _logger.LogInformation("Event created locally (offline): {EventId}", tempEvent.Id);
            return tempEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create event");
            return null;
        }
    }

    /// <summary>
    /// Update an event (offline-first)
    /// </summary>
    public async Task<EventResponse?> UpdateEventAsync(int eventId, UpdateEventRequest request)
    {
        try
        {
            if (_connectivityService.IsOnline)
            {
                // Try to update on server
                var serverEvent = await _apiService.UpdateEventAsync(eventId, request);
                
                if (serverEvent != null)
                {
                    // Cache event locally
                    await _localStorage.SaveEventAsync(serverEvent);
                    _logger.LogInformation("Event updated on server: {EventId}", eventId);
                    return serverEvent;
                }
            }

            // Offline: update local event and queue operation
            var localEvent = await _localStorage.GetEventAsync(eventId);
            if (localEvent != null)
            {
                // Update local event with new data
                UpdateLocalEvent(localEvent, request);
                await _localStorage.SaveEventAsync(localEvent);
                
                // Queue the operation for later sync
                var token = _authStateProvider.GetToken();
                await _syncService.QueueOperationAsync("PUT", $"/api/events/{eventId}", request, token);
                
                _logger.LogInformation("Event updated locally (offline): {EventId}", eventId);
                return localEvent;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update event {EventId}", eventId);
            return null;
        }
    }

    /// <summary>
    /// Delete an event (offline-first)
    /// </summary>
    public async Task<bool> DeleteEventAsync(int eventId)
    {
        try
        {
            if (_connectivityService.IsOnline)
            {
                // Try to delete on server
                try
                {
                    await _apiService.DeleteEventAsync(eventId);
                    
                    // Delete from local storage
                    await _localStorage.DeleteEventAsync(eventId);
                    _logger.LogInformation("Event deleted on server: {EventId}", eventId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete event on server: {EventId}", eventId);
                    // Fall through to offline mode
                }
            }

            // Offline: delete from local storage and queue operation
            await _localStorage.DeleteEventAsync(eventId);
            
            // Queue the operation for later sync
            var token = _authStateProvider.GetToken();
            await _syncService.QueueOperationAsync("DELETE", $"/api/events/{eventId}", null, token);
            
            _logger.LogInformation("Event deleted locally (offline): {EventId}", eventId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete event {EventId}", eventId);
            return false;
        }
    }

    /// <summary>
    /// Create a temporary event for offline use
    /// Uses negative ID to distinguish from server events
    /// </summary>
    private EventResponse CreateTemporaryEvent(CreateEventRequest request)
    {
        // Generate temporary negative ID (will be replaced when synced)
        var tempId = -(int)(DateTime.UtcNow.Ticks % int.MaxValue);

        return new EventResponse
        {
            Id = tempId,
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            IsAllDay = request.IsAllDay,
            Color = request.Color,
            Status = EventScheduler.Domain.Entities.EventStatus.Scheduled.ToString(),
            EventType = request.EventType?.ToString() ?? "Other",
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserId = 0, // Will be set by server
            CategoryId = request.CategoryId
        };
    }

    /// <summary>
    /// Update local event with request data
    /// </summary>
    private void UpdateLocalEvent(EventResponse localEvent, UpdateEventRequest request)
    {
        localEvent.Title = request.Title;
        localEvent.Description = request.Description;
        localEvent.StartDate = request.StartDate;
        localEvent.EndDate = request.EndDate;
        localEvent.Location = request.Location;
        localEvent.IsAllDay = request.IsAllDay;
        localEvent.Color = request.Color;
        localEvent.EventType = request.EventType?.ToString() ?? localEvent.EventType;
        localEvent.IsPublic = request.IsPublic;
        localEvent.UpdatedAt = DateTime.UtcNow;
        localEvent.CategoryId = request.CategoryId;
    }
}
