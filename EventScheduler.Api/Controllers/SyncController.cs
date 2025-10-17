using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using EventScheduler.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventScheduler.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(IEventService eventService, ILogger<SyncController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    [HttpPost("batch")]
    public async Task<IActionResult> BatchSync([FromBody] BatchSyncRequest request)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("Processing batch sync for user {UserId} with {Count} operations", 
                userId, request.Operations.Count);

            var response = new BatchSyncResponse
            {
                Results = new List<SyncOperationResult>(),
                Conflicts = new List<ConflictInfo>()
            };

            foreach (var operation in request.Operations.OrderBy(o => o.Timestamp))
            {
                try
                {
                    var result = await ProcessOperationAsync(userId, operation);
                    response.Results.Add(result);
                    
                    if (result.HasConflict)
                    {
                        response.Conflicts.Add(result.Conflict!);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing sync operation {OperationId}", operation.Id);
                    response.Results.Add(new SyncOperationResult
                    {
                        OperationId = operation.Id,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            _logger.LogInformation("Batch sync completed: {Success} successful, {Failed} failed, {Conflicts} conflicts",
                response.Results.Count(r => r.Success),
                response.Results.Count(r => !r.Success),
                response.Conflicts.Count);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch sync");
            return StatusCode(500, new { error = "An error occurred during batch sync" });
        }
    }

    private async Task<SyncOperationResult> ProcessOperationAsync(int userId, SyncOperation operation)
    {
        var result = new SyncOperationResult
        {
            OperationId = operation.Id,
            Success = false
        };

        try
        {
            switch (operation.Type.ToLowerInvariant())
            {
                case "create":
                    var createRequest = System.Text.Json.JsonSerializer.Deserialize<CreateEventRequest>(operation.Data);
                    if (createRequest != null)
                    {
                        var createdEvent = await _eventService.CreateEventAsync(userId, createRequest);
                        result.Success = true;
                        result.ServerId = createdEvent.Id;
                        result.ServerData = createdEvent;
                    }
                    break;

                case "update":
                    var updateData = System.Text.Json.JsonSerializer.Deserialize<UpdateEventData>(operation.Data);
                    if (updateData != null)
                    {
                        var existingEvent = await _eventService.GetEventByIdAsync(userId, updateData.Id);
                        
                        if (existingEvent != null)
                        {
                            if (existingEvent.LastModified > operation.Timestamp)
                            {
                                result.HasConflict = true;
                                result.Conflict = new ConflictInfo
                                {
                                    EventId = updateData.Id,
                                    LocalVersion = operation.Data,
                                    ServerVersion = System.Text.Json.JsonSerializer.Serialize(existingEvent),
                                    LocalTimestamp = operation.Timestamp,
                                    ServerTimestamp = existingEvent.LastModified
                                };
                            }

                            var updateRequest = new UpdateEventRequest
                            {
                                Title = updateData.Title,
                                Description = updateData.Description,
                                StartDate = updateData.StartDate,
                                EndDate = updateData.EndDate,
                                Location = updateData.Location,
                                IsAllDay = updateData.IsAllDay,
                                Color = updateData.Color,
                                EventType = updateData.EventType,
                                IsPublic = updateData.IsPublic
                            };
                            
                            await _eventService.UpdateEventAsync(userId, updateData.Id, updateRequest);
                            result.Success = true;
                            result.ServerId = updateData.Id;
                        }
                    }
                    break;

                case "delete":
                    var deleteData = System.Text.Json.JsonSerializer.Deserialize<DeleteEventData>(operation.Data);
                    if (deleteData != null)
                    {
                        await _eventService.DeleteEventAsync(userId, deleteData.EventId);
                        result.Success = true;
                    }
                    break;

                default:
                    result.ErrorMessage = $"Unknown operation type: {operation.Type}";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Operation} operation", operation.Type);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

public class BatchSyncRequest
{
    public List<SyncOperation> Operations { get; set; } = new();
}

public class SyncOperation
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class BatchSyncResponse
{
    public List<SyncOperationResult> Results { get; set; } = new();
    public List<ConflictInfo> Conflicts { get; set; } = new();
}

public class SyncOperationResult
{
    public string OperationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ServerId { get; set; }
    public bool HasConflict { get; set; }
    public ConflictInfo? Conflict { get; set; }
    public object? ServerData { get; set; }
}

public class ConflictInfo
{
    public int EventId { get; set; }
    public string LocalVersion { get; set; } = string.Empty;
    public string ServerVersion { get; set; } = string.Empty;
    public DateTime LocalTimestamp { get; set; }
    public DateTime ServerTimestamp { get; set; }
}

public class UpdateEventData
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public bool IsAllDay { get; set; }
    public string? Color { get; set; }
    public string EventType { get; set; } = "Other";
    public bool IsPublic { get; set; }
}

public class DeleteEventData
{
    public int EventId { get; set; }
}
