using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using Microsoft.JSInterop;
using System.Text.Json;

namespace EventScheduler.Web.Services;

public class OfflineStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<OfflineStorageService> _logger;
    private const string DB_NAME = "EventSchedulerOfflineDB";
    private const string EVENTS_STORE = "events";
    private const string PENDING_OPERATIONS_STORE = "pendingOperations";
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OfflineStorageService(IJSRuntime jsRuntime, ILogger<OfflineStorageService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("offlineStorage.initDB", DB_NAME, EVENTS_STORE, PENDING_OPERATIONS_STORE);
            _logger.LogInformation("Offline database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize offline database");
        }
    }

    public async Task SaveEventsAsync(List<EventResponse> events)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("offlineStorage.saveEvents", JsonSerializer.Serialize(events, JsonOptions));
            _logger.LogInformation("Saved {Count} events to offline storage", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save events to offline storage");
        }
    }

    public async Task<List<EventResponse>> GetEventsAsync()
    {
        try
        {
            var eventsJson = await _jsRuntime.InvokeAsync<string>("offlineStorage.getEvents");
            if (string.IsNullOrEmpty(eventsJson))
            {
                return new List<EventResponse>();
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var events = JsonSerializer.Deserialize<List<EventResponse>>(eventsJson, options) ?? new List<EventResponse>();
            _logger.LogInformation("Retrieved {Count} events from offline storage", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve events from offline storage");
            return new List<EventResponse>();
        }
    }

    public async Task AddPendingOperationAsync(PendingOperation operation)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("offlineStorage.addPendingOperation", JsonSerializer.Serialize(operation, JsonOptions));
            _logger.LogInformation("Added pending operation: {Type} for event {EventId}", operation.Type, operation.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add pending operation");
        }
    }

    public async Task<List<PendingOperation>> GetPendingOperationsAsync()
    {
        try
        {
            var operationsJson = await _jsRuntime.InvokeAsync<string>("offlineStorage.getPendingOperations");
            if (string.IsNullOrEmpty(operationsJson))
            {
                return new List<PendingOperation>();
            }

            using var doc = JsonDocument.Parse(operationsJson);
            var operations = new List<PendingOperation>();
            
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                try
                {
                    var operation = new PendingOperation
                    {
                        Id = element.GetProperty("Id").ValueKind == JsonValueKind.Number 
                            ? element.GetProperty("Id").GetInt64().ToString() 
                            : element.GetProperty("Id").GetString() ?? Guid.NewGuid().ToString(),
                        Type = element.GetProperty("Type").GetString() ?? "unknown",
                        EventId = element.TryGetProperty("EventId", out var eventIdProp) && eventIdProp.ValueKind == JsonValueKind.Number
                            ? eventIdProp.GetInt32()
                            : null,
                        EventData = element.TryGetProperty("Data", out var dataProp) 
                            ? dataProp.GetRawText() 
                            : null,
                        Timestamp = element.TryGetProperty("Timestamp", out var timestampProp)
                            ? DateTime.Parse(timestampProp.GetString() ?? DateTime.UtcNow.ToString())
                            : DateTime.UtcNow
                    };
                    operations.Add(operation);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse pending operation, skipping");
                }
            }
            
            _logger.LogInformation("Retrieved {Count} pending operations", operations.Count);
            return operations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve pending operations");
            return new List<PendingOperation>();
        }
    }

    public async Task RemovePendingOperationAsync(string operationId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("offlineStorage.removePendingOperation", operationId);
            _logger.LogInformation("Removed pending operation: {OperationId}", operationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove pending operation");
        }
    }

    public async Task ClearAllDataAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("offlineStorage.clearAll");
            _logger.LogInformation("Cleared all offline data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear offline data");
        }
    }
}

public class PendingOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public int? EventId { get; set; }
    public string? EventData { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
