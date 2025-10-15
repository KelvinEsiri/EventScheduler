using System.ComponentModel.DataAnnotations;

namespace EventScheduler.Application.DTOs.Request;

public class CreateEventRequest
{
    [Required(ErrorMessage = "Event title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public required string Title { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
    public string? Location { get; set; }

    public bool IsAllDay { get; set; }

    [StringLength(50)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex code (e.g., #FF5733)")]
    public string? Color { get; set; }

    public int? CategoryId { get; set; }

    public string EventType { get; set; } = "Other";

    public bool IsPublic { get; set; } = false;

    public List<EventInvitationRequest>? Invitations { get; set; }
}

public class EventInvitationRequest
{
    [Required(ErrorMessage = "Invitee name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    public required string InviteeName { get; set; }

    [Required(ErrorMessage = "Invitee email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public required string InviteeEmail { get; set; }
}
