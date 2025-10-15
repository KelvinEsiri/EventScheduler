# Route Naming Standardization

## Overview
This document describes the route naming conventions used across the EventScheduler application.

## Date
October 15, 2025

## Route Structure

### Primary Routes

| Route | Page | Component | Description |
|-------|------|-----------|-------------|
| `/` | Home | `Home.razor` | Landing page |
| `/login` | Login | `Login.razor` | User authentication |
| `/register` | Register | `Register.razor` | User registration |
| `/logout` | Logout | `Logout.razor` | User logout |
| `/calendar-list` | Calendar List | `CalendarList.razor` | List view of events |
| `/calendar-view` | Calendar View | `CalendarView.razor` | Grid/calendar view of events |

## Naming Convention

### Hyphenated Routes
All multi-word routes use hyphens (`-`) as separators:
- ✅ `/calendar-list`
- ✅ `/calendar-view`
- ❌ `/calendarlist` (old/inconsistent)
- ❌ `/calendarList` (camelCase not used)

### Rationale
1. **SEO-Friendly**: Hyphens are recognized as word separators by search engines
2. **Readability**: Easier to read URLs (e.g., `calendar-list` vs `calendarlist`)
3. **Web Standards**: Follows common web convention for URL slugs
4. **Consistency**: All routes follow the same pattern

## Route Changes History

### October 15, 2025
Standardized calendar routes from inconsistent naming to hyphenated format:

**Before:**
- Route definition: `/calendar-list`
- Navigation links: `/calendarlist` (no hyphen)
- Some references: `/calendar`

**After:**
- All references: `/calendar-list` (consistent everywhere)

### Files Updated
1. `CalendarList.razor` - `@page` directive
2. `MainLayout.razor` - Navigation link
3. `Register.razor` - Post-registration redirect
4. `CalendarView.razor` - "List View" button link

## Default Routes

### After Login
Users are redirected to `/calendar-view` (grid view) as the default landing page:
- `Login.razor`: Redirects to `/calendar-view`
- `App.razor`: Token validation redirect to `/calendar-view`
- `auth.js`: JavaScript redirect to `/calendar-view`

### After Registration
Users are redirected to `/calendar-list` after successful registration:
- `Register.razor`: Redirects to `/calendar-list`

## Navigation Flow

```
┌─────────────┐
│    Home     │
│     /       │
└──────┬──────┘
       │
   ┌───┴────┐
   │        │
┌──▼───┐ ┌──▼─────────┐
│Login │ │ Register   │
│/login│ │ /register  │
└──┬───┘ └──┬─────────┘
   │        │
   │    ┌───▼──────────────────┐
   │    │                      │
   │    │  Redirect based on   │
   │    │  authentication      │
   │    │                      │
   └────┼──────────────────────┘
        │
        │
   ┌────▼─────────────────────────┐
   │                              │
┌──▼──────────────┐  ┌───────────▼────────┐
│ Calendar List   │◄─┤  Calendar View     │
│ /calendar-list  │──►  /calendar-view    │
└─────────────────┘  └────────────────────┘
```

## API Endpoints vs Routes

### Web Routes (Frontend)
- Use hyphens: `/calendar-list`, `/calendar-view`
- Defined in Razor pages with `@page` directive

### API Endpoints (Backend)
- Use PascalCase controllers and methods
- Example: `/api/events`, `/api/auth/login`

## Route Parameters

Currently, the application does not use route parameters for calendar pages. All filtering and navigation is handled via:
- Query parameters (future enhancement)
- Component state management
- API calls with filters

## Future Enhancements

### Potential New Routes
- `/calendar-list/{year}/{month}` - Date-filtered list view
- `/calendar-view/{year}/{month}` - Date-specific calendar view
- `/event/{id}` - Individual event detail page
- `/event/{id}/edit` - Event editing page
- `/settings` - User settings page
- `/profile` - User profile page

### Route Guards
Consider implementing route guards for:
- Authentication-required routes
- Role-based access control
- Feature flags

## Testing

### Route Validation Checklist
- [ ] All navigation links use correct hyphenated format
- [ ] Direct URL access works for all routes
- [ ] Redirects function correctly after login/register
- [ ] Blazor routing recognizes all `@page` directives
- [ ] No 404 errors on valid routes
- [ ] Browser back/forward navigation works correctly

## Related Documentation
- [Button Layout Standardization](./BUTTON_LAYOUT_STANDARDIZATION.md)
- [Authentication Redirect Fix](./AUTH_REDIRECT_FIX.md)
- [Calendar Troubleshooting](./CALENDAR_TROUBLESHOOTING.md)

## Migration Notes

If you need to change a route:
1. Update the `@page` directive in the Razor component
2. Search and update all navigation links (`href=` attributes)
3. Update all `NavigationManager.NavigateTo()` calls
4. Update JavaScript redirects in `.js` files
5. Update documentation and README files
6. Test all navigation flows
7. Consider adding a redirect from old route to new route for backwards compatibility
