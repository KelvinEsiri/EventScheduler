using EventScheduler.Api.Hubs;
using EventScheduler.Application.DTOs.Response;
using EventScheduler.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace EventScheduler.Api.Services;

/// <summary>
/// Service for broadcasting event notifications via SignalR
/// Implements real-time communication for event CRUD operations
/// </summary>
public class EventNotificationService : IEventNotificationService
{
    private readonly IHubContext<EventHub> _hubContext;
    private readonly ILogger<EventNotificationService> _logger;

    public EventNotificationService(IHubContext<EventHub> hubContext, ILogger<EventNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Broadcasts a notification when a new event is created
    /// All connected clients receive the event data in real-time
    /// </summary>
    /// <param name="eventData">The newly created event details</param>
    public async Task NotifyEventCreatedAsync(EventResponse eventData)
    {
        try
        {
            _logger.LogInformation("📢 SignalR: Broadcasting EventCreated (ID: {EventId}, Title: '{Title}') to all clients...", 
                eventData.Id, eventData.Title);
            await _hubContext.Clients.All.SendAsync("EventCreated", eventData);
            _logger.LogInformation("✅ SignalR: EventCreated notification sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SignalR: Failed to send EventCreated notification");
        }
    }

    /// <summary>
    /// Broadcasts a notification when an event is updated
    /// All connected clients receive the updated event data
    /// </summary>
    /// <param name="eventData">The updated event details</param>
    public async Task NotifyEventUpdatedAsync(EventResponse eventData)
    {
        try
        {
            _logger.LogInformation("📢 SignalR: Broadcasting EventUpdated (ID: {EventId}, Title: '{Title}') to all clients...", 
                eventData.Id, eventData.Title);
            await _hubContext.Clients.All.SendAsync("EventUpdated", eventData);
            _logger.LogInformation("✅ SignalR: EventUpdated notification sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SignalR: Failed to send EventUpdated notification");
        }
    }

    /// <summary>
    /// Broadcasts a notification when an event is deleted
    /// Includes both event ID and title for client-side identification
    /// </summary>
    /// <param name="eventId">The ID of the deleted event</param>
    /// <param name="eventTitle">The title of the deleted event</param>
    public async Task NotifyEventDeletedAsync(int eventId, string eventTitle)
    {
        try
        {
            _logger.LogInformation("📢 SignalR: Broadcasting EventDeleted (ID: {EventId}, Title: '{Title}') to all clients...", 
                eventId, eventTitle);
            
            // Send both ID and title so clients can identify and remove the event
            await _hubContext.Clients.All.SendAsync("EventDeleted", new { id = eventId, title = eventTitle });
            _logger.LogInformation("✅ SignalR: EventDeleted notification sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SignalR: Failed to send EventDeleted notification");
        }
    }
}
