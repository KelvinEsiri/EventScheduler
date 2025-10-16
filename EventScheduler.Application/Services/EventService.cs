using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using EventScheduler.Application.Interfaces.Repositories;
using EventScheduler.Application.Interfaces.Services;
using EventScheduler.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EventScheduler.Application.Services;

/// <summary>
/// Service for managing event operations
/// Handles CRUD operations, invitations, and notifications for events
/// </summary>
public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<EventService> _logger;
    private readonly IEventNotificationService? _notificationService;

    public EventService(
        IEventRepository eventRepository, 
        IEmailService emailService, 
        IUserRepository userRepository,
        ILogger<EventService> logger,
        IEventNotificationService? notificationService = null)
    {
        _eventRepository = eventRepository;
        _emailService = emailService;
        _userRepository = userRepository;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Creates a new event with optional invitations
    /// Validates date ranges and sends notification emails to invitees
    /// </summary>
    /// <param name="userId">ID of the user creating the event</param>
    /// <param name="request">Event creation request with details and invitations</param>
    /// <returns>The created event details</returns>
    /// <exception cref="InvalidOperationException">Thrown when end date is before start date</exception>
    public async Task<EventResponse> CreateEventAsync(int userId, CreateEventRequest request)
    {
        _logger.LogInformation("Creating event '{Title}' for user {UserId}", request.Title, userId);
        
        if (request.EndDate < request.StartDate)
        {
            throw new InvalidOperationException("End date cannot be before start date");
        }

        // Parse the EventType string to enum
        if (!Enum.TryParse<EventType>(request.EventType, out var eventType))
        {
            eventType = EventType.Other;
        }

        var eventEntity = new Event
        {
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            IsAllDay = request.IsAllDay,
            Color = request.Color ?? "#007bff",
            CategoryId = request.CategoryId,
            UserId = userId,
            Status = EventStatus.Scheduled,
            EventType = eventType,
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow
        };

        var createdEvent = await _eventRepository.CreateAsync(eventEntity);
        _logger.LogInformation("Event {EventId} created successfully for user {UserId}", createdEvent.Id, userId);

        // Add invitations if any
        if (request.Invitations != null && request.Invitations.Any())
        {
            _logger.LogInformation("Adding {Count} invitations to event {EventId}", request.Invitations.Count, createdEvent.Id);
            foreach (var invitation in request.Invitations)
            {
                var invitationEntity = new EventInvitation
                {
                    EventId = createdEvent.Id,
                    InviteeName = invitation.InviteeName,
                    InviteeEmail = invitation.InviteeEmail,
                    InvitedAt = DateTime.UtcNow
                };
                createdEvent.Invitations.Add(invitationEntity);
            }
            await _eventRepository.UpdateAsync(createdEvent);
        }

        var eventResponse = MapToResponse(createdEvent);

        // Broadcast SignalR notification with event data
        if (_notificationService != null)
        {
            await _notificationService.NotifyEventCreatedAsync(eventResponse);
        }
        else
        {
            _logger.LogWarning("⚠️ SignalR: Notification service is null, cannot broadcast EventCreated");
        }

        return eventResponse;
    }

    public async Task<EventResponse> UpdateEventAsync(int userId, int eventId, UpdateEventRequest request)
    {
        _logger.LogInformation("Updating event {EventId} for user {UserId}", eventId, userId);
        
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, userId);
        
        if (eventEntity == null)
        {
            _logger.LogWarning("Event {EventId} not found for user {UserId}", eventId, userId);
            throw new InvalidOperationException("Event not found or you don't have permission to edit it");
        }

        if (request.EndDate < request.StartDate)
        {
            _logger.LogWarning("Invalid date range for event {EventId}: End date before start date", eventId);
            throw new InvalidOperationException("End date cannot be before start date");
        }

        // Parse the EventType string to enum
        if (!Enum.TryParse<EventType>(request.EventType, out var eventType))
        {
            eventType = EventType.Other;
        }

        eventEntity.Title = request.Title;
        eventEntity.Description = request.Description;
        eventEntity.StartDate = request.StartDate;
        eventEntity.EndDate = request.EndDate;
        eventEntity.Location = request.Location;
        eventEntity.IsAllDay = request.IsAllDay;
        eventEntity.Color = request.Color ?? eventEntity.Color;
        eventEntity.CategoryId = request.CategoryId;
        eventEntity.EventType = eventType;
        eventEntity.IsPublic = request.IsPublic;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<EventStatus>(request.Status, out var status))
        {
            var oldStatus = eventEntity.Status;
            eventEntity.Status = status;

            if (status == EventStatus.Completed && oldStatus != EventStatus.Completed)
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    await _emailService.SendEventCompletedEmailAsync(user.Email, user.FullName, eventEntity.Title);
                }
            }
        }

        // Update invitations
        if (request.Invitations != null)
        {
            eventEntity.Invitations.Clear();
            foreach (var invitation in request.Invitations)
            {
                var invitationEntity = new EventInvitation
                {
                    EventId = eventEntity.Id,
                    InviteeName = invitation.InviteeName,
                    InviteeEmail = invitation.InviteeEmail,
                    InvitedAt = DateTime.UtcNow
                };
                eventEntity.Invitations.Add(invitationEntity);
            }
        }

        await _eventRepository.UpdateAsync(eventEntity);
        _logger.LogInformation("Event {EventId} updated successfully by user {UserId}", eventId, userId);

        var eventResponse = MapToResponse(eventEntity);

        // Broadcast SignalR notification with updated event data
        if (_notificationService != null)
        {
            await _notificationService.NotifyEventUpdatedAsync(eventResponse);
        }
        else
        {
            _logger.LogWarning("⚠️ SignalR: Notification service is null, cannot broadcast EventUpdated");
        }

        return eventResponse;
    }

    public async Task DeleteEventAsync(int userId, int eventId)
    {
        _logger.LogInformation("Deleting event {EventId} for user {UserId}", eventId, userId);
        
        // Get event details before deletion for notification
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, userId);
        var eventTitle = eventEntity?.Title ?? "Unknown Event";
        
        await _eventRepository.DeleteAsync(eventId, userId);
        _logger.LogInformation("Event {EventId} deleted successfully", eventId);

        // Broadcast SignalR notification with event ID and title
        if (_notificationService != null)
        {
            await _notificationService.NotifyEventDeletedAsync(eventId, eventTitle);
        }
        else
        {
            _logger.LogWarning("⚠️ SignalR: Notification service is null, cannot broadcast EventDeleted");
        }
    }

    public async Task<EventResponse?> GetEventByIdAsync(int userId, int eventId)
    {
        _logger.LogDebug("Getting event {EventId} for user {UserId}", eventId, userId);
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, userId);
        return eventEntity == null ? null : MapToResponse(eventEntity);
    }

    public async Task<IEnumerable<EventResponse>> GetAllEventsAsync(int userId)
    {
        _logger.LogDebug("Getting all events for user {UserId}", userId);
        var events = await _eventRepository.GetAllAsync(userId);
        _logger.LogInformation("Retrieved {Count} events for user {UserId}", events.Count(), userId);
        return events.Select(MapToResponse);
    }

    public async Task<IEnumerable<EventResponse>> GetEventsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        _logger.LogDebug("Getting events for user {UserId} between {StartDate} and {EndDate}", userId, startDate, endDate);
        var events = await _eventRepository.GetByDateRangeAsync(userId, startDate, endDate);
        _logger.LogInformation("Retrieved {Count} events for user {UserId} in date range", events.Count(), userId);
        return events.Select(MapToResponse);
    }

    public async Task<IEnumerable<EventResponse>> GetPublicEventsAsync()
    {
        _logger.LogDebug("Getting public events");
        var events = await _eventRepository.GetPublicEventsAsync();
        _logger.LogInformation("Retrieved {Count} public events", events.Count());
        return events.Select(MapToResponse);
    }

    public async Task<EventResponse?> GetPublicEventByIdAsync(int eventId)
    {
        _logger.LogDebug("Getting public event {EventId}", eventId);
        var eventEntity = await _eventRepository.GetPublicEventByIdAsync(eventId);
        return eventEntity == null ? null : MapToResponse(eventEntity);
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
            EventType = eventEntity.EventType.ToString(),
            IsPublic = eventEntity.IsPublic,
            UserId = eventEntity.UserId,
            CategoryId = eventEntity.CategoryId,
            CategoryName = eventEntity.Category?.Name,
            ParticipantsCount = eventEntity.Invitations?.Count ?? 0,
            Invitations = eventEntity.Invitations?.Select(i => new EventInvitationResponse
            {
                Id = i.Id,
                InviteeName = i.InviteeName,
                InviteeEmail = i.InviteeEmail,
                InvitedAt = i.InvitedAt
            }).ToList(),
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt
        };
    }
}
