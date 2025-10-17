using EventScheduler.Domain.Entities;

namespace EventScheduler.Application.DTOs.Response;

public class EventResponse
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public bool IsAllDay { get; set; }
    public string? Color { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int UserId { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int ParticipantsCount { get; set; }
    public List<EventInvitationResponse>? Invitations { get; set; }
    public List<EventAttendeeResponse>? Attendees { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime LastModified => UpdatedAt ?? CreatedAt;
    public bool IsJoined { get; set; }
    public string? CreatedByUserName { get; set; }
    public bool IsJoinedEvent { get; set; }
    public int? OriginalEventId { get; set; }
}

public class EventInvitationResponse
{
    public int Id { get; set; }
    public string InviteeName { get; set; } = string.Empty;
    public string? InviteeEmail { get; set; }
    public DateTime InvitedAt { get; set; }
    public int? UserId { get; set; }
}
