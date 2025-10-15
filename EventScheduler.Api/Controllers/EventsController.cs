using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventScheduler.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllEvents()
    {
        try
        {
            var userId = GetUserId();
            var events = await _eventService.GetAllEventsAsync(userId);
            return Ok(events);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events");
            return StatusCode(500, new { error = "An error occurred while retrieving events" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEventById(int id)
    {
        try
        {
            var userId = GetUserId();
            var eventData = await _eventService.GetEventByIdAsync(userId, id);
            
            if (eventData == null)
            {
                return NotFound(new { error = "Event not found" });
            }

            return Ok(eventData);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event {EventId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the event" });
        }
    }

    [HttpGet("date-range")]
    public async Task<IActionResult> GetEventsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var userId = GetUserId();
            var events = await _eventService.GetEventsByDateRangeAsync(userId, startDate, endDate);
            return Ok(events);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events by date range");
            return StatusCode(500, new { error = "An error occurred while retrieving events" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var eventData = await _eventService.CreateEventAsync(userId, request);
            _logger.LogInformation("Event {EventId} created by user {UserId}", eventData.Id, userId);
            
            return CreatedAtAction(nameof(GetEventById), new { id = eventData.Id }, eventData);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return StatusCode(500, new { error = "An error occurred while creating the event" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var eventData = await _eventService.UpdateEventAsync(userId, id, request);
            _logger.LogInformation("Event {EventId} updated by user {UserId}", id, userId);
            
            return Ok(eventData);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the event" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        try
        {
            var userId = GetUserId();
            await _eventService.DeleteEventAsync(userId, id);
            _logger.LogInformation("Event {EventId} deleted by user {UserId}", id, userId);
            
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the event" });
        }
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicEvents()
    {
        try
        {
            var events = await _eventService.GetPublicEventsAsync();
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public events");
            return StatusCode(500, new { error = "An error occurred while retrieving public events" });
        }
    }

    [AllowAnonymous]
    [HttpGet("public/{id}")]
    public async Task<IActionResult> GetPublicEventById(int id)
    {
        try
        {
            var eventData = await _eventService.GetPublicEventByIdAsync(id);
            
            if (eventData == null)
            {
                return NotFound(new { error = "Public event not found" });
            }

            return Ok(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public event {EventId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the public event" });
        }
    }
}
