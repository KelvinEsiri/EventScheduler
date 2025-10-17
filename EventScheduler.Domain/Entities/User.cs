namespace EventScheduler.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool EmailVerified { get; set; } = false;
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Navigation properties
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public ICollection<EventInvitation> EventInvitations { get; set; } = new List<EventInvitation>();
    public ICollection<EventAttendee> AttendingEvents { get; set; } = new List<EventAttendee>();
}
