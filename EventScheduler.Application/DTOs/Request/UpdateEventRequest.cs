namespace EventScheduler.Application.DTOs.Request;

public class UpdateEventRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public bool IsAllDay { get; set; }
    public string? Color { get; set; }
    public int? CategoryId { get; set; }
    public string? Status { get; set; }
}
