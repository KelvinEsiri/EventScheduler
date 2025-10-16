using EventScheduler.Application.DTOs.Response;

namespace EventScheduler.Application.Interfaces.Services;

/// <summary>
/// Interface for SignalR event notification service
/// </summary>
public interface IEventNotificationService
{
    Task NotifyEventCreatedAsync(EventResponse eventData);
    Task NotifyEventUpdatedAsync(EventResponse eventData);
    Task NotifyEventDeletedAsync(int eventId, string eventTitle);
}
