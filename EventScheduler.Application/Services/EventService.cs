using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using EventScheduler.Application.Interfaces.Repositories;
using EventScheduler.Application.Interfaces.Services;
using EventScheduler.Domain.Entities;

namespace EventScheduler.Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;

    public EventService(IEventRepository eventRepository, IEmailService emailService, IUserRepository userRepository)
    {
        _eventRepository = eventRepository;
        _emailService = emailService;
        _userRepository = userRepository;
    }

    public async Task<EventResponse> CreateEventAsync(int userId, CreateEventRequest request)
    {
        var eventEntity = new Event
        {
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            IsAllDay = request.IsAllDay,
            Color = request.Color,
            CategoryId = request.CategoryId,
            UserId = userId,
            Status = EventStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        var createdEvent = await _eventRepository.CreateAsync(eventEntity);

        return MapToResponse(createdEvent);
    }

    public async Task<EventResponse> UpdateEventAsync(int userId, int eventId, UpdateEventRequest request)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, userId);
        
        if (eventEntity == null)
        {
            throw new InvalidOperationException("Event not found");
        }

        eventEntity.Title = request.Title;
        eventEntity.Description = request.Description;
        eventEntity.StartDate = request.StartDate;
        eventEntity.EndDate = request.EndDate;
        eventEntity.Location = request.Location;
        eventEntity.IsAllDay = request.IsAllDay;
        eventEntity.Color = request.Color;
        eventEntity.CategoryId = request.CategoryId;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<EventStatus>(request.Status, out var status))
        {
            var oldStatus = eventEntity.Status;
            eventEntity.Status = status;

            // Send email notification if event is marked as completed
            if (status == EventStatus.Completed && oldStatus != EventStatus.Completed)
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    await _emailService.SendEventCompletedEmailAsync(user.Email, user.FullName, eventEntity.Title);
                }
            }
        }

        await _eventRepository.UpdateAsync(eventEntity);

        return MapToResponse(eventEntity);
    }

    public async Task DeleteEventAsync(int userId, int eventId)
    {
        await _eventRepository.DeleteAsync(eventId, userId);
    }

    public async Task<EventResponse?> GetEventByIdAsync(int userId, int eventId)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, userId);
        return eventEntity == null ? null : MapToResponse(eventEntity);
    }

    public async Task<IEnumerable<EventResponse>> GetAllEventsAsync(int userId)
    {
        var events = await _eventRepository.GetAllAsync(userId);
        return events.Select(MapToResponse);
    }

    public async Task<IEnumerable<EventResponse>> GetEventsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var events = await _eventRepository.GetByDateRangeAsync(userId, startDate, endDate);
        return events.Select(MapToResponse);
    }

    private EventResponse MapToResponse(Event eventEntity)
    {
        return new EventResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            Location = eventEntity.Location,
            IsAllDay = eventEntity.IsAllDay,
            Color = eventEntity.Color,
            Status = eventEntity.Status.ToString(),
            UserId = eventEntity.UserId,
            CategoryId = eventEntity.CategoryId,
            CategoryName = eventEntity.Category?.Name,
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt
        };
    }
}
