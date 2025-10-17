# Toggle Alignment Fix - CalendarList

## Issue
The "Public Event" toggle in the CalendarList (My Events) page was misaligned compared to the CalendarView page. The toggles appeared in separate rows instead of being side-by-side.

## Root Cause
The CalendarList.razor form had a different structure than CalendarView.razor:

**CalendarList (Before):**
- "All Day Event" toggle in its own `form-group` (full-width)
- Event Type dropdown in a `form-group`
- "Public Event" toggle in another `form-group`
- Result: Toggles appeared in **separate rows**

**CalendarView (Correct):**
- Event Type dropdown in a `form-group`
- Both toggles together in a `form-group` with `toggle-row` container
- Result: Toggles appeared **side-by-side**

## Solution Applied

### Changed CalendarList.razor Structure

Reorganized the form to match CalendarView:

```razor
<!-- Event Type -->
<div class="form-group">
    <label class="form-label">
        <i class="bi bi-tag"></i>
        Event Type
    </label>
    <InputSelect class="form-control" @bind-Value="eventRequest.EventType">
        <!-- options -->
    </InputSelect>
</div>

<!-- Quick Toggles -->
<div class="form-group full-width">
    <div class="toggle-row">
        <div class="toggle-group">
            <InputCheckbox class="toggle-input" id="isAllDay" @bind-Value="eventRequest.IsAllDay" />
            <label class="toggle-label" for="isAllDay">
                <div class="toggle-slider"></div>
                <span class="toggle-text">
                    <i class="bi bi-sun"></i>
                    All Day Event
                </span>
            </label>
        </div>
        
        <div class="toggle-group">
            <InputCheckbox class="toggle-input" id="isPublic" @bind-Value="eventRequest.IsPublic" />
            <label class="toggle-label" for="isPublic">
                <div class="toggle-slider"></div>
                <span class="toggle-text">
                    <i class="bi bi-globe"></i>
                    Public Event
                </span>
            </label>
        </div>
    </div>
</div>
```

### Key Changes
1. **Moved Event Type dropdown** before the toggles
2. **Wrapped both toggles** in a single `form-group` with `full-width` class
3. **Added `toggle-row` container** to display toggles side-by-side
4. Both pages now use the **same structure**

## Result
✅ Both toggles now appear **side-by-side** on the same row
✅ Consistent form layout between CalendarView and CalendarList
✅ Better use of horizontal space
✅ Improved visual alignment

## CSS Support
The required CSS classes already existed in `wwwroot/css/calendar.css`:
- `.toggle-row` - Flexbox container with gap
- `.toggle-group` - Individual toggle wrapper
- `.toggle-label`, `.toggle-slider` - Toggle styling

## Files Modified
- `EventScheduler.Web/Components/Pages/CalendarList.razor`

## Testing
- [x] Verify toggles appear side-by-side
- [ ] Test toggle functionality still works
- [ ] Check responsive behavior on mobile
- [ ] Compare with CalendarView form for consistency
