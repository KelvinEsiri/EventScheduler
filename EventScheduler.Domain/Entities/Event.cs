namespace EventScheduler.Domain.Entities;

public class Event
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public bool IsAllDay { get; set; } = false;
    public string? Color { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public int? CategoryId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public EventCategory? Category { get; set; }
}

public enum EventStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}
