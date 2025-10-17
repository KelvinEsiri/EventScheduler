# Created By Feature Implementation Summary

## Overview
Added "Created By" information to events and implemented functionality for users to join public events by creating their own copy, with restrictions on editing joined events.

## Changes Made

### 1. Database Schema Changes

#### Event Entity (EventScheduler.Domain/Entities/Event.cs)
- Added `CreatedByUserId` (int?) - Stores the ID of the original event creator
- Added `OriginalEventId` (int?) - Links to the original public event if this is a joined copy
- Added `CreatedByUserName` (string?) - Stores the creator's name for display purposes

#### Migrations
- Created migration: `AddCreatedByAndOriginalEventIdToEvent`
- Created migration: `AddCreatedByUserNameToEvent`

### 2. DTOs

#### EventResponse (EventScheduler.Application/DTOs/Response/EventResponse.cs)
- Added `CreatedByUserName` (string?) - Display name of the event creator
- Added `IsJoinedEvent` (bool) - Indicates if this event is a copy from a public event
- Added `OriginalEventId` (int?) - ID of the original public event (for joined events)

### 3. Service Layer Changes

#### EventService (EventScheduler.Application/Services/EventService.cs)

**MapToResponse Method:**
- Updated to populate `CreatedByUserName` from the User navigation property for original events
- Uses stored `CreatedByUserName` field for joined events
- Sets `IsJoinedEvent` flag based on presence of `OriginalEventId`
- Includes `OriginalEventId` in response

**JoinPublicEventAsync Method:**
- Changed from adding invitation to creating a complete copy of the event
- New event copy includes:
  - All event details (title, description, dates, location, etc.)
  - `UserId` set to the joining user
  - `CreatedByUserId` set to the original event creator's ID
  - `CreatedByUserName` set to the original creator's name
  - `OriginalEventId` set to the public event ID
  - `IsPublic` set to false (user's copy is private)
- Checks if user already has a copy before creating a new one

**LeaveEventAsync Method:**
- Changed from removing invitation to deleting the user's copy of the event
- Finds and deletes the event with matching `OriginalEventId` and user's ID

**UpdateEventAsync Method:**
- Added validation to prevent editing joined events
- Throws `InvalidOperationException` with message: "Cannot edit joined events. You can only delete them."

### 4. Repository Changes

#### EventRepository (EventScheduler.Infrastructure/Repositories/EventRepository.cs)
- Added `.Include(e => e.User)` to all queries that need creator information:
  - `GetByIdAsync`
  - `GetAllAsync`
  - `GetByDateRangeAsync`
  - `GetPublicEventsAsync` (already included)
  - `GetPublicEventByIdAsync` (already included)

### 5. UI Changes

#### PublicEvents.razor
**Event List View:**
- Added "Created by" display in event detail items:
  ```razor
  @if (!string.IsNullOrEmpty(evt.CreatedByUserName))
  {
      <div class="event-detail-item">
          <i class="bi bi-person-circle"></i>
          <span>Created by @evt.CreatedByUserName</span>
      </div>
  }
  ```

**Event Details Modal:**
- Added "Created By" row in details grid with same icon and format

#### PublicEvents.razor.cs
- Added `userEvents` list to track user's personal events
- Added `LoadUserEvents()` method to fetch user's events for joined status checking
- Updated `IsUserJoined()` to check if user has a copy with matching `OriginalEventId`
- Updated `JoinEvent()` to reload user events after joining
- Updated `LeaveEvent()` to reload user events after leaving

#### CalendarList.razor
- Added "Created By" display in event detail items (same format as PublicEvents)

## Feature Behavior

### For Public Events:
1. **Display:** Shows the original creator's name for all public events
2. **Joining:** Creates a complete copy of the event in the user's personal calendar
3. **Joined Status:** Button changes from "Join Event" to "Leave Event" if user has joined

### For Joined Events:
1. **Display:** Shows who created the original event
2. **Editing:** NOT allowed - users can only delete joined events
3. **Privacy:** User's copy is private (not shared back to public)
4. **Tracking:** Maintains link to original event via `OriginalEventId`

### Delete Functionality:
- Users can delete their own created events
- Users can delete joined event copies (via "Leave Event")
- Deleting a joined copy does NOT affect the original public event

## API Endpoints Affected

- `POST /api/events/public/{id}/join` - Now creates event copy instead of adding invitation
- `POST /api/events/public/{id}/leave` - Now deletes event copy instead of removing invitation
- `PUT /api/events/{id}` - Now validates that event is not a joined copy before allowing edit

## Migration Commands

```bash
cd EventScheduler.Infrastructure
dotnet ef migrations add AddCreatedByAndOriginalEventIdToEvent --startup-project ..\EventScheduler.Api\EventScheduler.Api.csproj
dotnet ef migrations add AddCreatedByUserNameToEvent --startup-project ..\EventScheduler.Api\EventScheduler.Api.csproj
dotnet ef database update --startup-project ..\EventScheduler.Api\EventScheduler.Api.csproj
```

## Testing Checklist

- [ ] Public event displays creator name
- [ ] Join button creates a copy in user's calendar
- [ ] Joined event shows creator name in user's calendar
- [ ] Leave button removes copy from user's calendar
- [ ] Cannot edit joined events (only delete)
- [ ] Can edit own created events normally
- [ ] Original public event unchanged when users join/leave
- [ ] Joined status correctly displayed after joining
- [ ] Multiple users can join the same public event independently

## Notes

- The invitation system is still in place for regular events
- This change only affects the public event join/leave functionality
- User's joined event copies are private and independent of the original
- Creator information is stored redundantly (CreatedByUserName) for performance
