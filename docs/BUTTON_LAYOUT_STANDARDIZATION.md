# Button Layout Standardization

## Overview
This document describes the standardization of button layouts across the Calendar List and Calendar View pages to provide a consistent user experience.

## Date
October 15, 2025

## Changes Made

### Button Order Standardization
Both Calendar List (`/calendar-list`) and Calendar View (`/calendar-view`) pages now have identical button ordering in their headers:

1. **View Toggle Button** (left) - Outline style
2. **New Event Button** (right) - Primary style

### Page-Specific Implementations

#### Calendar List Page (`/calendar-list`)
```razor
<div class="header-actions">
    <a href="/calendar-view" class="btn btn-outline btn-calendar">
        <i class="bi bi-calendar-heart"></i>
        <span>Calendar View</span>
    </a>
    <button class="btn btn-primary btn-create" @onclick="ShowCreateModal">
        <i class="bi bi-plus-lg"></i>
        <span>New Event</span>
    </button>
</div>
```

#### Calendar View Page (`/calendar-view`)
```razor
<div class="header-actions">
    <a href="/calendar-list" class="btn btn-outline btn-list">
        <i class="bi bi-list-task"></i>
        <span>List View</span>
    </a>
    <button class="btn btn-primary btn-create" @onclick="ShowCreateModal">
        <i class="bi bi-plus-lg"></i>
        <span>New Event</span>
    </button>
</div>
```

## Design Rationale

### Consistent Positioning
- **View Toggle First**: Allows users to quickly switch between Calendar and List views
- **New Event Second**: Primary action button is positioned consistently on the right
- **Visual Hierarchy**: Outline button (secondary action) → Primary button (main action)

### CSS Layout
Both pages use identical CSS for button positioning:

```css
.header-actions {
    display: flex;
    gap: 1rem;
    flex-wrap: wrap;
}
```

### Header Structure
Both pages share the same header structure:
- Gradient background (`#667eea` to `#764ba2`)
- Flexbox layout with `justify-content: space-between`
- Max-width: `1400px`
- Responsive wrapping for mobile devices

## User Experience Benefits

1. **Consistency**: Users see buttons in the same location regardless of which page they're on
2. **Predictability**: Once users learn the layout on one page, they know where to find buttons on the other
3. **Visual Balance**: View toggle (outline) and New Event (primary) create a balanced visual hierarchy
4. **Accessibility**: Consistent button order improves navigation for keyboard and screen reader users

## Responsive Behavior

On mobile devices (< 768px):
- Buttons stack vertically with `flex-wrap: wrap`
- Full-width buttons for easier touch interaction
- Maintain the same order (view toggle, then new event)

## Related Files

### Pages
- `EventScheduler.Web/Components/Pages/CalendarList.razor`
- `EventScheduler.Web/Components/Pages/CalendarView.razor`

### Routing
- Calendar List: `/calendar-list`
- Calendar View: `/calendar-view`

### Navigation Links
- `EventScheduler.Web/Components/Layout/MainLayout.razor` - Main navigation
- `EventScheduler.Web/Components/Pages/Register.razor` - Post-registration redirect

## Future Considerations

- Consider extracting header buttons into a shared component to reduce code duplication
- Add consistent hover states and animations across both pages
- Consider adding keyboard shortcuts for quick view switching (e.g., `Ctrl+Shift+C` for Calendar, `Ctrl+Shift+L` for List)

## Testing Notes

Verify the following:
1. Buttons appear in the same position on both pages
2. "New Event" button opens the create modal on both pages
3. View toggle buttons navigate correctly:
   - "Calendar View" button navigates to `/calendar-view`
   - "List View" button navigates to `/calendar-list`
4. Responsive layout works on mobile devices
5. Button styling is consistent (colors, sizing, spacing)

## See Also
- [Route Naming Standardization](./ROUTE_NAMING_STANDARDIZATION.md)
- [Architecture Documentation](./ARCHITECTURE.md)
- [Calendar Troubleshooting](./CALENDAR_TROUBLESHOOTING.md)
