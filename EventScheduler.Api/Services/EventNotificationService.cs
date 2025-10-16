using EventScheduler.Api.Hubs;
using EventScheduler.Application.DTOs.Response;
using EventScheduler.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace EventScheduler.Api.Services;

public class EventNotificationService : IEventNotificationService
{
    private readonly IHubContext<EventHub> _hubContext;
    private readonly ILogger<EventNotificationService> _logger;

    public EventNotificationService(IHubContext<EventHub> hubContext, ILogger<EventNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

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
