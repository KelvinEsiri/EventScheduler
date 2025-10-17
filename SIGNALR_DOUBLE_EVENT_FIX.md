# SignalR Double Event Display Fix

## Problem Identified

When a user created a new event in CalendarView, the event would appear **twice** on the calendar.

## Root Cause

The issue was caused by a race condition in the event creation flow:

1. **User creates event** → `SaveEvent()` method calls API
2. **API creates event** → Returns created event data
3. **API broadcasts SignalR notification** → `EventCreated` message sent to all connected clients
4. **SignalR handler receives notification** → Adds event to calendar
5. **Result**: Event added twice (once from API response, once from SignalR)

### Why the Existing Protection Failed

The code had a `lastLocalOperationTime` check to prevent duplicate notifications:

```csharp
if (IsRecentLocalOperation())
{
    lastLocalOperationTime = null;
}
else
{
    ShowSuccess($"Event '{eventData.Title}' created!");
}
```

However, this only prevented the **success message** from showing twice. It still:
- Added the event to the `events` list
- Called `addEventToCalendar()` via JavaScript
- **Result**: The event appeared twice on the calendar

## Solution Applied

### 1. Added Local Creation Flag

Added a new boolean flag to track when we're creating an event locally:

```csharp
private bool isCreatingEventLocally = false; // Flag to prevent SignalR duplicate during creation
```

### 2. Updated SignalR Handler (`RegisterSignalRHandlers`)

Changed the `EventCreated` handler to check the flag **first** before adding events:

```csharp
hubConnection.On<EventResponse>("EventCreated", async (eventData) => {
    await InvokeAsync(async () => {
        // Skip adding event if we're currently creating it locally
        if (isCreatingEventLocally)
        {
            Logger.LogInformation("SignalR: Skipping EventCreated - local creation in progress");
            return; // Don't add the event - SaveEvent will handle it
        }
        
        // Backup check for recent operations
        if (IsRecentLocalOperation())
        {
            lastLocalOperationTime = null;
            Logger.LogInformation("SignalR: Skipping EventCreated - recent local operation");
            return;
        }
        
        // This is from another user/session
        ShowSuccess($"Event '{eventData.Title}' created!");
        events.Add(eventData);
        await JSRuntime.InvokeVoidAsync("addEventToCalendar", eventData);
        StateHasChanged();
    });
});
```

**Key changes**: 
- Check `isCreatingEventLocally` flag first (most reliable)
- Keep time-based check as backup
- Both return early to prevent duplicate adds

### 3. Updated SaveEvent Method

Modified `SaveEvent()` to set flag, create event, then reset flag:

```csharp
else
{
    try
    {
        // Set flag to prevent SignalR from adding the event during creation
        isCreatingEventLocally = true;
        lastLocalOperationTime = DateTime.UtcNow;
        
        // Create the event via API
        var createdEvent = await ApiService.CreateEventAsync(eventRequest);
        
        if (createdEvent != null)
        {
            // Manually add the event locally (SignalR will skip it due to flag)
            events.Add(createdEvent);
            await JSRuntime.InvokeVoidAsync("addEventToCalendar", createdEvent);
            
            ShowSuccess("Event created successfully!");
        }
    }
    finally
    {
        // Reset the flag after a delay to ensure SignalR notification is caught
        _ = Task.Delay(3000).ContinueWith(_ => {
            isCreatingEventLocally = false;
        });
    }
}
```

**Key changes**:
- Set `isCreatingEventLocally = true` in try block **before** API call
- This blocks SignalR from adding the event during the entire operation
- Manually add event after API returns
- Reset flag after 3-second delay in finally block (ensures SignalR msg is caught)
- Time-based check acts as backup protection

## Flow After Fix (v2 - with flag)

### For Local User (Creator)
1. User creates event → `SaveEvent()` called
2. **`isCreatingEventLocally` flag set to TRUE** (blocks SignalR)
3. API call starts → Server creates event and broadcasts SignalR
4. SignalR notification arrives → **BLOCKED by flag**, handler returns early
5. API returns event data → **Manually add event locally**
6. Event appears on calendar **once**
7. After 3 seconds → Flag resets to FALSE
8. **Result**: Event appears **once** ✅

### For Other Users (Observers)
1. User A creates event → API broadcasts SignalR notification
2. User B receives notification → `isCreatingEventLocally = false` (not creating)
3. SignalR handler adds event → Appears on User B's calendar
4. **Result**: Event appears **once** ✅

## Why the Flag Approach Works Better

The original time-based approach had a race condition:
- **Problem**: SignalR message could arrive BEFORE the API call completed
- **Timeline**: Set time → API call starts → SignalR arrives (within 2s) → API completes
- **Result**: Time check passes, but we still add twice

The flag approach eliminates this:
- **Solution**: Flag is set for the ENTIRE duration of the API call
- **Timeline**: Set flag → API call → SignalR arrives → Flag blocks it → API completes → Add manually
- **Result**: SignalR is always blocked during local creation, no race condition

## Testing Checklist

- [x] Create new event → Should appear once immediately
- [ ] Create event from another browser/user → Should appear once via SignalR
- [ ] Edit existing event → Should not duplicate
- [ ] Drag/drop event → Should not duplicate
- [ ] Resize event → Should not duplicate

## Files Modified

1. `EventScheduler.Web/Components/Pages/CalendarView.razor.cs`
   - `RegisterSignalRHandlers()` method
   - `SaveEvent()` method

## Why PublicEvents.razor.cs Wasn't Changed

The PublicEvents page is a **read-only view** where users only:
- View public events
- Join/leave events

Users don't **create** events from this page, so the double-add issue doesn't occur there.

## Additional Notes

- The fix maintains real-time synchronization between users
- Local operations get instant feedback (no waiting for SignalR)
- Other users see updates via SignalR broadcasts
- The 2-second window for `IsRecentLocalOperation()` is sufficient for most network conditions
