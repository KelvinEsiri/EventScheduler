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

        // Prevent editing joined events (events that were copied from public events)
        if (eventEntity.OriginalEventId.HasValue)
        {
            _logger.LogWarning("User {UserId} attempted to edit joined event {EventId} (original: {OriginalId})", userId, eventId, eventEntity.OriginalEventId.Value);
            throw new InvalidOperationException("Cannot edit joined events. You can only delete them.");
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

        // Auto-update status based on date changes
        var now = DateTime.UtcNow;
        var autoStatusChanged = false;
        
        if (eventEntity.Status == EventStatus.Late && request.EndDate > now)
        {
            // If rescheduled to future date, revert from Late to Scheduled
            eventEntity.Status = EventStatus.Scheduled;
            autoStatusChanged = true;
            _logger.LogInformation("Event {EventId} rescheduled to future, status changed from Late to Scheduled", eventId);
        }
        else if (eventEntity.Status == EventStatus.Scheduled && request.EndDate < now)
        {
            // If scheduled event is moved to past, mark as Late
            eventEntity.Status = EventStatus.Late;
            autoStatusChanged = true;
            _logger.LogInformation("Event {EventId} rescheduled to past, status changed from Scheduled to Late", eventId);
        }

        // Manual status override (only if no auto status change occurred)
        if (!autoStatusChanged && !string.IsNullOrEmpty(request.Status) && Enum.TryParse<EventStatus>(request.Status, out var status))
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
        
        // If this is a joined event (has OriginalEventId), remove user from attendees
        if (eventEntity?.OriginalEventId.HasValue == true)
        {
            var originalEventId = eventEntity.OriginalEventId.Value;
            var originalEvent = await _eventRepository.GetPublicEventByIdAsync(originalEventId);
            
            if (originalEvent != null)
            {
                var attendee = originalEvent.Attendees.FirstOrDefault(a => a.UserId == userId);
                if (attendee != null)
                {
                    originalEvent.Attendees.Remove(attendee);
                    await _eventRepository.UpdateAsync(originalEvent);
                    _logger.LogInformation("Removed user {UserId} from attendees of original event {OriginalEventId}", userId, originalEventId);
                }
            }
        }
        
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
        
        // Update late events automatically
        var now = DateTime.UtcNow;
        var eventsToUpdate = new List<Event>();
        
        foreach (var eventEntity in events)
        {
            if (eventEntity.Status == EventStatus.Scheduled && eventEntity.EndDate < now)
            {
                eventEntity.Status = EventStatus.Late;
                eventsToUpdate.Add(eventEntity);
                _logger.LogInformation("Event {EventId} automatically marked as Late", eventEntity.Id);
            }
        }
        
        // Batch update all late events if any
        if (eventsToUpdate.Any())
        {
            foreach (var eventEntity in eventsToUpdate)
            {
                await _eventRepository.UpdateAsync(eventEntity);
            }
        }
        
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

    public async Task<EventResponse> JoinPublicEventAsync(int userId, int eventId)
    {
        _logger.LogInformation("User {UserId} attempting to join event {EventId}", userId, eventId);
        
        var publicEvent = await _eventRepository.GetPublicEventByIdAsync(eventId);
        
        if (publicEvent == null || !publicEvent.IsPublic)
        {
            _logger.LogWarning("Event {EventId} not found or not public", eventId);
            throw new InvalidOperationException("Event not found or is not public");
        }

        // Prevent users from joining their own events
        if (publicEvent.UserId == userId)
        {
            _logger.LogWarning("User {UserId} attempted to join their own event {EventId}", userId, eventId);
            throw new InvalidOperationException("You cannot join your own event");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            throw new InvalidOperationException("User not found");
        }

        // Check if user already has a copy of this event
        var userEvents = await _eventRepository.GetAllAsync(userId);
        var existingJoinedEvent = userEvents.FirstOrDefault(e => e.OriginalEventId == eventId);
        if (existingJoinedEvent != null)
        {
            _logger.LogInformation("User {UserId} already has a copy of event {EventId}", userId, eventId);
            return MapToResponse(existingJoinedEvent);
        }

        // Add user to the public event's attendees list
        var existingAttendee = publicEvent.Attendees.FirstOrDefault(a => a.UserId == userId);
        if (existingAttendee == null)
        {
            var attendee = new EventAttendee
            {
                EventId = eventId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };
            publicEvent.Attendees.Add(attendee);
            await _eventRepository.UpdateAsync(publicEvent);
            _logger.LogInformation("User {UserId} added to attendees list of event {EventId}", userId, eventId);
        }

        // Create a copy of the event for the user
        var joinedEvent = new Event
        {
            Title = publicEvent.Title,
            Description = publicEvent.Description,
            StartDate = publicEvent.StartDate,
            EndDate = publicEvent.EndDate,
            Location = publicEvent.Location,
            IsAllDay = publicEvent.IsAllDay,
            Color = publicEvent.Color,
            Status = publicEvent.Status,
            EventType = publicEvent.EventType,
            IsPublic = false, // User's copy is private
            CategoryId = publicEvent.CategoryId,
            UserId = userId, // This event belongs to the joining user
            CreatedByUserId = publicEvent.UserId, // Track original creator ID
            CreatedByUserName = publicEvent.User?.FullName, // Store creator name directly
            OriginalEventId = eventId, // Link to original public event
            CreatedAt = DateTime.UtcNow
        };

        var createdEvent = await _eventRepository.CreateAsync(joinedEvent);
        _logger.LogInformation("User {UserId} successfully joined event {EventId}, created copy with Id {CopyId}", userId, eventId, createdEvent.Id);
        
        return MapToResponse(createdEvent);
    }

    public async Task LeaveEventAsync(int userId, int eventId)
    {
        _logger.LogInformation("User {UserId} attempting to leave event {EventId}", userId, eventId);
        
        // Find the user's copy of the joined event
        var userEvents = await _eventRepository.GetAllAsync(userId);
        var joinedEventCopy = userEvents.FirstOrDefault(e => e.OriginalEventId == eventId && e.UserId == userId);
        
        if (joinedEventCopy == null)
        {
            _logger.LogWarning("User {UserId} has not joined event {EventId}", userId, eventId);
            throw new InvalidOperationException("You have not joined this event");
        }

        // Remove user from the public event's attendees list
        var publicEvent = await _eventRepository.GetPublicEventByIdAsync(eventId);
        if (publicEvent != null)
        {
            var attendee = publicEvent.Attendees.FirstOrDefault(a => a.UserId == userId);
            if (attendee != null)
            {
                publicEvent.Attendees.Remove(attendee);
                await _eventRepository.UpdateAsync(publicEvent);
                _logger.LogInformation("User {UserId} removed from attendees list of event {EventId}", userId, eventId);
            }
        }

        // Delete the user's copy of the event
        await _eventRepository.DeleteAsync(joinedEventCopy.Id, userId);
        _logger.LogInformation("User {UserId} successfully left event {EventId} by deleting copy {CopyId}", userId, eventId, joinedEventCopy.Id);
    }

    public async Task UpdateEventStatusesAsync()
    {
        _logger.LogInformation("Updating event statuses based on dates");
        
        var now = DateTime.UtcNow;
        var allUsers = await _userRepository.GetAllAsync();
        
        foreach (var user in allUsers)
        {
            var userEvents = await _eventRepository.GetAllAsync(user.Id);
            
            foreach (var eventEntity in userEvents)
            {
                if (eventEntity.Status == EventStatus.Scheduled && eventEntity.EndDate < now)
                {
                    eventEntity.Status = EventStatus.Late;
                    await _eventRepository.UpdateAsync(eventEntity);
                    _logger.LogInformation("Event {EventId} marked as Late", eventEntity.Id);
                }
            }
        }
    }

    private EventResponse MapToResponse(Event eventEntity)
    {
        // Determine creator username
        string? createdByUserName = null;
        
        if (eventEntity.OriginalEventId.HasValue)
        {
            // This is a joined event - use the stored CreatedByUserName
            createdByUserName = eventEntity.CreatedByUserName;
        }
        else if (eventEntity.User != null)
        {
            // For original events (not joined), show the owner as creator
            createdByUserName = eventEntity.User.FullName;
        }

        return new EventResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            Location = eventEntity.Location,
            IsAllDay = eventEntity.IsAllDay,
            Color = GetEventColor(eventEntity),
            Status = eventEntity.Status.ToString(),
            EventType = eventEntity.EventType.ToString(),
            IsPublic = eventEntity.IsPublic,
            UserId = eventEntity.UserId,
            CategoryId = eventEntity.CategoryId,
            CategoryName = eventEntity.Category?.Name,
            ParticipantsCount = eventEntity.IsPublic 
                ? (eventEntity.Attendees?.Count ?? 0) // Use attendees count for public events
                : (eventEntity.Invitations?.Count ?? 0), // Use invitations count for private events
            Invitations = eventEntity.Invitations?.Select(i => new EventInvitationResponse
            {
                Id = i.Id,
                InviteeName = i.InviteeName,
                InviteeEmail = i.InviteeEmail,
                InvitedAt = i.InvitedAt,
                UserId = i.UserId
            }).ToList(),
            Attendees = eventEntity.Attendees?.Select(a => new EventAttendeeResponse
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User?.FullName ?? "Unknown",
                UserEmail = a.User?.Email,
                JoinedAt = a.JoinedAt
            }).ToList(),
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt,
            IsJoined = false,
            CreatedByUserName = createdByUserName,
            IsJoinedEvent = eventEntity.OriginalEventId.HasValue,
            OriginalEventId = eventEntity.OriginalEventId
        };
    }

    private string GetEventColor(Event eventEntity)
    {
        // Always calculate color based on status and type for consistency
        // (Users can't customize colors per-event, only per-type)
        var calculatedColor = eventEntity.Status switch
        {
            EventStatus.Completed => "#10b981", // Green
            EventStatus.Cancelled => "#ef4444", // Red
            EventStatus.InProgress => "#f59e0b", // Amber
            EventStatus.Late => "#f59e0b", // Amber
            _ => eventEntity.EventType switch // Default by event type
            {
                EventType.Festival => "#ec4899", // Pink
                EventType.Interview => "#8b5cf6", // Purple
                EventType.Birthday => "#f97316", // Orange
                EventType.Exam => "#dc2626", // Dark Red
                EventType.Appointment => "#06b6d4", // Cyan
                EventType.Meeting => "#3b82f6", // Blue
                EventType.Reminder => "#eab308", // Yellow
                EventType.Task => "#14b8a6", // Teal
                _ => "#6366f1" // Indigo (default for Other)
            }
        };
        
        _logger.LogInformation("🎨 Calculated color for event {EventId} (Type: {EventType}, Status: {Status}): {Color}", 
            eventEntity.Id, eventEntity.EventType, eventEntity.Status, calculatedColor);
        
        return calculatedColor;
    }
}
