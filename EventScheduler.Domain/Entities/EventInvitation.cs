namespace EventScheduler.Domain.Entities;

/// <summary>
/// Represents an invitation/participation in an event
/// </summary>
public class EventInvitation
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public required string InviteeName { get; set; }
    public required string InviteeEmail { get; set; }
    public int? UserId { get; set; }
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Event Event { get; set; } = null!;
    public User? User { get; set; }
}
