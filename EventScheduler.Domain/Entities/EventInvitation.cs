namespace EventScheduler.Domain.Entities;

/// <summary>
/// Represents an invitation to a private event
/// </summary>
public class EventInvitation
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public required string InviteeName { get; set; }
    public required string InviteeEmail { get; set; }
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Event Event { get; set; } = null!;
}
