using Microsoft.JSInterop;
using EventScheduler.Application.DTOs.Response;
using System.Text.Json;

namespace EventScheduler.Web.Services;

/// <summary>
/// Service for managing local storage using IndexedDB
/// Provides offline-first data persistence
/// </summary>
public class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IJSRuntime jsRuntime, ILogger<LocalStorageService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Initialize IndexedDB
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("indexedDBManager.initDB");
            _logger.LogInformation("IndexedDB initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize IndexedDB");
        }
    }

    #region Event Operations

    /// <summary>
    /// Save an event to local storage
    /// </summary>
    public async Task<EventResponse?> SaveEventAsync(EventResponse eventData)
    {
        try
        {
            // Just save the event, don't try to deserialize the result
            // The JavaScript returns the event with metadata that may cause deserialization issues
            await _jsRuntime.InvokeVoidAsync("indexedDBManager.saveEvent", eventData);
            
            _logger.LogInformation("Event saved to local storage: {EventId}", eventData.Id);
            
            // Return the original event data since we know it's valid
            return eventData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save event to local storage: {EventId}", eventData.Id);
            return null;
        }
    }

    /// <summary>
    /// Get all events for a user from local storage
    /// </summary>
    public async Task<List<EventResponse>> GetAllEventsAsync(int? userId = null)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<JsonElement>(
                "indexedDBManager.getAllEvents", userId);
            
            var events = JsonSerializer.Deserialize<List<EventResponse>>(result.GetRawText()) 
                ?? new List<EventResponse>();
            
            _logger.LogInformation("Retrieved {Count} events from local storage", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events from local storage");
            return new List<EventResponse>();
        }
    }

    /// <summary>
    /// Get a single event from local storage
    /// </summary>
    public async Task<EventResponse?> GetEventAsync(int eventId)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<JsonElement>(
                "indexedDBManager.getEvent", eventId);
            
            if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            var eventData = JsonSerializer.Deserialize<EventResponse>(result.GetRawText());
            _logger.LogInformation("Retrieved event from local storage: {EventId}", eventId);
            return eventData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get event from local storage: {EventId}", eventId);
            return null;
        }
    }

    /// <summary>
    /// Delete an event from local storage
    /// </summary>
    public async Task<bool> DeleteEventAsync(int eventId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("indexedDBManager.deleteEvent", eventId);
            _logger.LogInformation("Event deleted from local storage: {EventId}", eventId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete event from local storage: {EventId}", eventId);
            return false;
        }
    }

    #endregion

    #region Pending Operations

    /// <summary>
    /// Save a pending operation to sync later
    /// </summary>
    public async Task<int> SavePendingOperationAsync(PendingOperation operation)
    {
        try
        {
            var operationId = await _jsRuntime.InvokeAsync<int>(
                "indexedDBManager.savePendingOperation", operation);
            
            _logger.LogInformation("Pending operation saved: {OperationId} - {Type}", 
                operationId, operation.Type);
            return operationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save pending operation: {Type}", operation.Type);
            return 0;
        }
    }

    /// <summary>
    /// Get all pending operations
    /// </summary>
    public async Task<List<PendingOperation>> GetPendingOperationsAsync()
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<JsonElement>(
                "indexedDBManager.getPendingOperations");
            
            var operations = JsonSerializer.Deserialize<List<PendingOperation>>(result.GetRawText()) 
                ?? new List<PendingOperation>();
            
            _logger.LogInformation("Retrieved {Count} pending operations", operations.Count);
            return operations;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
        {
            _logger.LogWarning("Cannot get pending operations during prerendering - returning empty list");
            return new List<PendingOperation>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get pending operations - returning empty list");
            return new List<PendingOperation>();
        }
    }

    /// <summary>
    /// Delete a pending operation
    /// </summary>
    public async Task<bool> DeletePendingOperationAsync(int operationId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "indexedDBManager.deletePendingOperation", operationId);
            
            _logger.LogInformation("Pending operation deleted: {OperationId}", operationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete pending operation: {OperationId}", operationId);
            return false;
        }
    }

    /// <summary>
    /// Clear all pending operations
    /// </summary>
    public async Task<bool> ClearPendingOperationsAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("indexedDBManager.clearPendingOperations");
            _logger.LogInformation("All pending operations cleared");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear pending operations");
            return false;
        }
    }

    #endregion

    #region Sync Metadata

    /// <summary>
    /// Save sync metadata
    /// </summary>
    public async Task<bool> SaveSyncMetadataAsync(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "indexedDBManager.saveSyncMetadata", key, value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save sync metadata: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Get sync metadata
    /// </summary>
    public async Task<string?> GetSyncMetadataAsync(string key)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>(
                "indexedDBManager.getSyncMetadata", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sync metadata: {Key}", key);
            return null;
        }
    }

    #endregion

    /// <summary>
    /// Clear all local data (use on logout)
    /// </summary>
    public async Task<bool> ClearAllDataAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("indexedDBManager.clearAllData");
            _logger.LogInformation("All local data cleared");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all local data");
            return false;
        }
    }
}

/// <summary>
/// Represents a pending operation to be synced with the server
/// </summary>
public class PendingOperation
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // POST, PUT, DELETE
    public string Endpoint { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? Token { get; set; }
    public DateTime Timestamp { get; set; }
}
