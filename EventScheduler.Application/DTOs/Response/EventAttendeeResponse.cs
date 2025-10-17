namespace EventScheduler.Application.DTOs.Response;

public class EventAttendeeResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public DateTime JoinedAt { get; set; }
}
