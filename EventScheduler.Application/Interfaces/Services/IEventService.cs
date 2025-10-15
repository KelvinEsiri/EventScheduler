using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;

namespace EventScheduler.Application.Interfaces.Services;

public interface IEventService
{
    Task<EventResponse> CreateEventAsync(int userId, CreateEventRequest request);
    Task<EventResponse> UpdateEventAsync(int userId, int eventId, UpdateEventRequest request);
    Task DeleteEventAsync(int userId, int eventId);
    Task<EventResponse?> GetEventByIdAsync(int userId, int eventId);
    Task<IEnumerable<EventResponse>> GetAllEventsAsync(int userId);
    Task<IEnumerable<EventResponse>> GetEventsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<EventResponse>> GetPublicEventsAsync();
    Task<EventResponse?> GetPublicEventByIdAsync(int eventId);
}
