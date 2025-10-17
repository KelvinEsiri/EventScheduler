# Shared Services Documentation

## Overview
This document describes shared business logic services that have been extracted from Blazor components to improve code maintainability, reduce duplication, and follow the DRY (Don't Repeat Yourself) principle.

---

## EventUIHelperService

**Location:** `EventScheduler.Web/Services/EventUIHelperService.cs`  
**Registration:** Scoped (in `Program.cs`)  
**Purpose:** Centralizes UI helper methods for event rendering across multiple components

### Problem Solved
Before this refactoring, the following methods were **duplicated** across three components:
- `CalendarView.razor.cs` - 15 lines of duplicate code
- `CalendarList.razor.cs` - 28 lines of duplicate code  
- `PublicEvents.razor.cs` - 32 lines of duplicate code

**Total duplicate code removed:** ~75 lines

### Methods

#### 1. GetEventTypeIcon(string eventType)
Returns the emoji icon for a given event type.

**Usage:**
```csharp
@UIHelper.GetEventTypeIcon("Meeting")  // Returns: 👥
@UIHelper.GetEventTypeIcon("Birthday")  // Returns: 🎂
```

**Supported Event Types:**
| Event Type | Icon |
|-----------|------|
| Festival | 🎉 |
| Interview | 💼 |
| Birthday | 🎂 |
| Exam | 📝 |
| Appointment | 🏥 |
| Meeting | 👥 |
| Reminder | ⏰ |
| Task | ✅ |
| Other | 📅 |

---

#### 2. GetEventTypeClass(string eventType)
Returns the CSS class for styling event cards based on event type.

**Usage:**
```razor
<div class="event-card @UIHelper.GetEventTypeClass(evt.EventType)">
```

**Output Examples:**
- `"festival"` → `"event-type-festival"`
- `"meeting"` → `"event-type-meeting"`
- `"birthday"` → `"event-type-birthday"`

These classes are styled in `wwwroot/css/pages/public-events.css` with unique border colors.

---

#### 3. GetEventStatusClass(string status)
Returns Bootstrap border class based on event status.

**Usage:**
```razor
<div class="event-card @UIHelper.GetEventStatusClass(evt.Status)">
```

**Status Mapping:**
| Status | CSS Class | Color |
|--------|-----------|-------|
| Completed | border-success | Green |
| Cancelled | border-danger | Red |
| InProgress | border-warning | Orange |
| Late | border-danger | Red |
| Scheduled | border-primary | Blue |

---

#### 4. GetStatusBadgeClass(string status)
Returns CSS class for status badges.

**Usage:**
```razor
<span class="@UIHelper.GetStatusBadgeClass(evt.Status)">@evt.Status</span>
```

**Output Examples:**
- `"Scheduled"` → `"status-scheduled"`
- `"InProgress"` → `"status-inprogress"`
- `"Completed"` → `"status-completed"`

---

#### 5. FormatDateRange(DateTime startDate, DateTime endDate, bool isAllDay)
Formats a date range for display with smart formatting.

**Usage:**
```csharp
// All day event
UIHelper.FormatDateRange(startDate, endDate, true)
// Output: "Oct 17, 2025 (All Day)"

// Same day event
UIHelper.FormatDateRange(
    new DateTime(2025, 10, 17, 9, 0, 0),
    new DateTime(2025, 10, 17, 11, 0, 0),
    false
)
// Output: "Oct 17, 2025 • 09:00 AM - 11:00 AM"

// Multi-day event
UIHelper.FormatDateRange(
    new DateTime(2025, 10, 17, 9, 0, 0),
    new DateTime(2025, 10, 19, 17, 0, 0),
    false
)
// Output: "Oct 17, 2025 09:00 AM - Oct 19, 2025 05:00 PM"
```

---

#### 6. FormatTimeRange(DateTime startDate, DateTime endDate, bool isAllDay)
Formats time range for display (time portion only).

**Usage:**
```csharp
// All day event
UIHelper.FormatTimeRange(startDate, endDate, true)
// Output: "All Day"

// Timed event
UIHelper.FormatTimeRange(
    new DateTime(2025, 10, 17, 9, 0, 0),
    new DateTime(2025, 10, 17, 11, 0, 0),
    false
)
// Output: "09:00 AM - 11:00 AM"
```

---

## How to Use in Components

### 1. Inject the Service
In your `.razor.cs` code-behind file:

```csharp
public partial class YourComponent
{
    [Inject] private EventUIHelperService UIHelper { get; set; } = default!;
    
    // ... rest of your component code
}
```

### 2. Use in Razor Markup
In your `.razor` file:

```razor
@* Display event type icon *@
<span>@UIHelper.GetEventTypeIcon(evt.EventType) @evt.EventType</span>

@* Apply CSS class based on event type *@
<div class="event-card @UIHelper.GetEventTypeClass(evt.EventType)">
    ...
</div>

@* Apply border class based on status *@
<div class="event-card @UIHelper.GetEventStatusClass(evt.Status)">
    ...
</div>

@* Format date range *@
<p>@UIHelper.FormatDateRange(evt.StartDate, evt.EndDate, evt.IsAllDay)</p>
```

---

## Components Using This Service

### ✅ CalendarView
**File:** `Components/Pages/CalendarView.razor`  
**Usage:**
- Event type icons in event detail modal
- Event type icons in day events list

### ✅ CalendarList  
**File:** `Components/Pages/CalendarList.razor`  
**Usage:**
- Event status border classes on event cards
- Event type icons in event badges

### ✅ PublicEvents
**File:** `Components/Pages/PublicEvents.razor`  
**Usage:**
- Event type CSS classes for card styling
- Event type icons in event badges and modals

---

## Benefits of This Approach

### 1. **Single Source of Truth**
- All UI formatting logic is in one place
- Changes to icons or classes only need to be made once
- Reduces risk of inconsistencies across the application

### 2. **Easier Maintenance**
- Adding a new event type? Update one method, not three components
- Changing an icon? Update one switch statement
- Clear separation of concerns

### 3. **Testability**
- Service can be unit tested independently
- Mock service easily for component testing
- No UI logic buried in components

### 4. **Reusability**
- Any new component can immediately use these helpers
- No need to copy-paste code
- Promotes consistency across the application

### 5. **Better Code Organization**
- Components focus on component logic
- UI helpers are grouped together
- Follows SOLID principles (Single Responsibility)

---

## Future Enhancements

Potential additions to `EventUIHelperService`:

### 1. Color Management
```csharp
public string GetEventTypeColor(string eventType)
{
    return eventType switch
    {
        "Festival" => "#f59e0b",
        "Interview" => "#3b82f6",
        "Birthday" => "#ec4899",
        // ... etc
    };
}
```

### 2. Priority Indicators
```csharp
public string GetPriorityIcon(string priority)
{
    return priority switch
    {
        "High" => "🔴",
        "Medium" => "🟡",
        "Low" => "🟢",
        _ => "⚪"
    };
}
```

### 3. Duration Formatting
```csharp
public string FormatDuration(DateTime startDate, DateTime endDate)
{
    var duration = endDate - startDate;
    
    if (duration.TotalDays >= 1)
        return $"{duration.Days} day(s)";
    if (duration.TotalHours >= 1)
        return $"{duration.Hours} hour(s)";
    
    return $"{duration.Minutes} minute(s)";
}
```

### 4. Participant Count Formatting
```csharp
public string FormatParticipantCount(int count)
{
    return count switch
    {
        0 => "No participants",
        1 => "1 participant",
        _ => $"{count} participants"
    };
}
```

---

## Related Documentation

- **Code-Behind Refactoring:** See `CODE_BEHIND_REFACTORING.md`
- **Project Structure:** See `PROJECT_STRUCTURE.md`
- **Architecture:** See `docs/ARCHITECTURE.md`

---

## Conclusion

The `EventUIHelperService` demonstrates best practices for managing shared business logic in Blazor applications:

✅ Eliminates code duplication  
✅ Improves maintainability  
✅ Enhances testability  
✅ Follows SOLID principles  
✅ Centralizes UI logic  

This pattern should be applied to other areas of the application where similar duplication exists.
