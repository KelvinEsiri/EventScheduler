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
    public int UserId { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
