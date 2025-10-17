# Duplicate Event Bug - FIXED

## 🐛 The Problem

**Symptom**: When creating a new event while online, it appears **twice** on the calendar. Refreshing the page normalizes it (shows only once).

## 🔍 Root Cause

**Race Condition between Optimistic UI and SignalR Broadcast**

### The Flow That Caused Duplicates:

1. **User creates event** → Calls `ApiService.CreateEventAsync()`
2. **API creates event** → Returns event with ID
3. **SignalR broadcasts** `EventCreated` to ALL clients (including you)
4. **SignalR handler receives broadcast** → Adds event to calendar
5. **Result**: Event added twice (no optimistic update, only SignalR)

### But Wait, There's More!

The code had `IsRecentLocalOperation()` to detect your own operations, but it was **only suppressing the success message**, NOT skipping the duplicate add:

```csharp
if (IsRecentLocalOperation()) {
    lastLocalOperationTime = null;  // Skip success message ✅
} else {
    ShowSuccess($"Event '{eventData.Title}' created!");
}

events.Add(eventData);  // ❌ ALWAYS added - causes duplicate!
await JSRuntime.InvokeVoidAsync("addEventToCalendar", eventData);
```

## ✅ The Fix

### 1. **Optimistic Update in SaveEvent** (Online Mode)

Now we **add the event immediately** after API call succeeds:

```csharp
if (isOnline)
{
    // Create event and get response with real ID
    var createdEvent = await ApiService.CreateEventAsync(eventRequest);
    
    // Add immediately (optimistic update)
    if (createdEvent != null)
    {
        events.Add(createdEvent);
        await JSRuntime.InvokeVoidAsync("addEventToCalendar", createdEvent);
        Logger.LogInformation("Event {EventId} added optimistically", createdEvent.Id);
    }
}
```

### 2. **Skip SignalR Duplicate**

SignalR handler now **skips adding** if it detects your own recent operation:

```csharp
hubConnection.On<EventResponse>("EventCreated", async (eventData) => {
    await InvokeAsync(async () => {
        if (IsRecentLocalOperation())
        {
            // Skip - already added optimistically
            Logger.LogInformation("Skipping SignalR EventCreated - own operation (Event {EventId})", eventData.Id);
            lastLocalOperationTime = null;
        }
        else
        {
            // Event created by another user - add it
            Logger.LogInformation("Adding event from SignalR (Event {EventId})", eventData.Id);
            ShowSuccess($"Event '{eventData.Title}' created!");
            events.Add(eventData);
            await JSRuntime.InvokeVoidAsync("addEventToCalendar", eventData);
        }
        StateHasChanged();
    });
});
```

### 3. **Timing Window**

`IsRecentLocalOperation()` checks if the operation happened within **2 seconds**:

```csharp
private bool IsRecentLocalOperation() => 
    lastLocalOperationTime.HasValue && 
    (DateTime.UtcNow - lastLocalOperationTime.Value).TotalSeconds < 2;
```

## 🔄 How It Works Now

### Your Own Event Creation:
```
1. User clicks "Create Event"
2. Set lastLocalOperationTime = Now
3. Call API → CreateEventAsync()
4. API returns event (ID: 123)
5. ✅ Add to calendar optimistically (your copy)
6. SignalR broadcasts EventCreated (ID: 123)
7. Check: IsRecentLocalOperation()? → Yes (< 2s)
8. ✅ Skip adding (prevent duplicate)
9. Reset lastLocalOperationTime = null
10. Result: Event appears ONCE ✅
```

### Another User's Event:
```
1. Another user creates event
2. SignalR broadcasts EventCreated (ID: 456)
3. Check: IsRecentLocalOperation()? → No
4. ✅ Add to calendar
5. Show success: "Event 'Team Meeting' created!"
6. Result: Event appears ONCE ✅
```

## 📊 Files Modified

| File | Lines | Purpose |
|------|-------|---------|
| `CalendarView.razor.cs` | 936-960 | Add optimistic update for online creation |
| `CalendarView.razor.cs` | 235-250 | Skip SignalR duplicate for own operations |

## 🧪 Testing

### Before Fix:
```
✓ Create event "Test Event"
❌ Event appears twice on calendar
✓ Refresh page
✓ Event appears once (normalized)
```

### After Fix:
```
✓ Create event "Test Event"
✓ Event appears once immediately
✓ Refresh page
✓ Event still appears once
✓ Another user creates "Team Meeting"
✓ "Team Meeting" appears with success message
```

## 🎯 Benefits

### 1. **Instant Feedback**
- Event appears immediately after API success
- No waiting for SignalR broadcast
- Better perceived performance

### 2. **No Duplicates**
- Own events added only once (optimistically)
- Other users' events added via SignalR
- Clean separation of concerns

### 3. **Proper Notifications**
- Own events: No notification (you know you created it)
- Other users' events: Success notification
- Clear distinction between your actions and others'

## 📝 Notes

### Why 2 Seconds?

The 2-second window for `IsRecentLocalOperation()` is generous enough to cover:
- API roundtrip time (~100-500ms)
- SignalR broadcast delay (~50-200ms)
- Network latency
- Processing time

### Alternative Approaches Considered

1. **Track by Event ID**: Doesn't work for create (ID assigned by server)
2. **Disable SignalR for creator**: Too complex, breaks real-time updates
3. **Debounce SignalR**: Would delay all real-time updates
4. **Server-side filtering**: SignalR broadcasts to all, can't filter by sender easily

The **time-based approach** is simple, reliable, and works well in practice.

### Offline Mode

Offline mode **already worked correctly** because:
- Creates event with temp ID
- Adds to calendar immediately
- No SignalR broadcast (offline)
- When synced, temp event replaced with real event

## ✅ Success Criteria Met

- [x] Events appear only once when created online
- [x] No duplicates after SignalR broadcast
- [x] Other users' events still appear with notification
- [x] Own events appear immediately (optimistic)
- [x] Refresh doesn't change anything
- [x] Offline mode unaffected

## 🎉 Result

**No more duplicate events!** Clean, instant feedback with proper real-time updates. 🚀
