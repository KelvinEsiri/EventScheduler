namespace EventScheduler.Web.Services;

/// <summary>
/// Provides shared UI helper methods for event rendering across components.
/// Centralizes event type icons, CSS classes, and status styling to maintain consistency
/// and reduce code duplication in CalendarView, CalendarList, and PublicEvents components.
/// </summary>
public class EventUIHelperService
{
    /// <summary>
    /// Gets the emoji icon for a given event type
    /// </summary>
    /// <param name="eventType">The event type (e.g., "Festival", "Meeting", "Birthday")</param>
    /// <returns>Unicode emoji representing the event type</returns>
    public string GetEventTypeIcon(string eventType)
    {
        return eventType switch
        {
            "Festival" => "🎉",
            "Interview" => "💼",
            "Birthday" => "🎂",
            "Exam" => "📝",
            "Appointment" => "🏥",
            "Meeting" => "👥",
            "Reminder" => "⏰",
            "Task" => "✅",
            _ => "📅"
        };
    }

    /// <summary>
    /// Gets the CSS class for styling event cards based on event type
    /// </summary>
    /// <param name="eventType">The event type</param>
    /// <returns>CSS class name for event type styling</returns>
    public string GetEventTypeClass(string eventType)
    {
        return eventType.ToLower() switch
        {
            "festival" => "event-type-festival",
            "interview" => "event-type-interview",
            "birthday" => "event-type-birthday",
            "exam" => "event-type-exam",
            "appointment" => "event-type-appointment",
            "meeting" => "event-type-meeting",
            "reminder" => "event-type-reminder",
            "task" => "event-type-task",
            _ => "event-type-other"
        };
    }

    /// <summary>
    /// Gets the Bootstrap border CSS class based on event status
    /// </summary>
    /// <param name="status">The event status (e.g., "Completed", "Cancelled", "InProgress")</param>
    /// <returns>Bootstrap border class for status styling</returns>
    public string GetEventStatusClass(string status)
    {
        return status switch
        {
            "Completed" => "border-success",
            "Cancelled" => "border-danger",
            "InProgress" => "border-warning",
            "Late" => "border-danger",
            _ => "border-primary"
        };
    }

    /// <summary>
    /// Gets the CSS class for status badges
    /// </summary>
    /// <param name="status">The event status</param>
    /// <returns>CSS class name for status badge styling</returns>
    public string GetStatusBadgeClass(string status)
    {
        return status.ToLower() switch
        {
            "scheduled" => "status-scheduled",
            "inprogress" => "status-inprogress",
            "completed" => "status-completed",
            "cancelled" => "status-cancelled",
            "late" => "status-late",
            _ => "status-scheduled"
        };
    }

    /// <summary>
    /// Formats a date range for display
    /// </summary>
    /// <param name="startDate">Event start date</param>
    /// <param name="endDate">Event end date</param>
    /// <param name="isAllDay">Whether the event is all day</param>
    /// <returns>Formatted date range string</returns>
    public string FormatDateRange(DateTime startDate, DateTime endDate, bool isAllDay)
    {
        if (isAllDay)
        {
            return startDate.ToString("MMM dd, yyyy") + " (All Day)";
        }
        
        if (startDate.Date == endDate.Date)
        {
            // Same day event
            return $"{startDate.ToString("MMM dd, yyyy")} • {startDate.ToString("hh:mm tt")} - {endDate.ToString("hh:mm tt")}";
        }
        else
        {
            // Multi-day event
            return $"{startDate.ToString("MMM dd, yyyy hh:mm tt")} - {endDate.ToString("MMM dd, yyyy hh:mm tt")}";
        }
    }

    /// <summary>
    /// Formats time range for display (time portion only)
    /// </summary>
    /// <param name="startDate">Event start date</param>
    /// <param name="endDate">Event end date</param>
    /// <param name="isAllDay">Whether the event is all day</param>
    /// <returns>Formatted time range string</returns>
    public string FormatTimeRange(DateTime startDate, DateTime endDate, bool isAllDay)
    {
        if (isAllDay)
        {
            return "All Day";
        }
        
        return $"{startDate.ToString("hh:mm tt")} - {endDate.ToString("hh:mm tt")}";
    }
}
