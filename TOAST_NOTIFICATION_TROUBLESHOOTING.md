# 🔧 Toast Notification Troubleshooting Guide

## 🎯 Quick Diagnosis Checklist

### ✅ If Notifications Still Don't Appear

#### 1. **Check Browser Console** (F12)
Look for JavaScript errors:
```
Right-click → Inspect → Console tab
```

Common issues:
- Missing CSS file references
- JavaScript initialization errors
- SignalR connection failures

#### 2. **Verify SignalR Connection**
In the CalendarView page, check the **connection indicator**:
- 🟢 **Green WiFi icon** = Connected (notifications will work)
- 🔴 **Red WiFi-off icon** = Disconnected (notifications won't work)

#### 3. **Check Browser Network Tab**
1. Open DevTools (F12)
2. Go to Network tab
3. Look for `/hubs/events/negotiate`
4. Should return **200 OK**

#### 4. **Verify Authentication**
Toast notifications require a logged-in user:
- Ensure you're logged in
- Check if token exists (check browser localStorage)

#### 5. **Check CSS Loading**
The toast styles should be in:
```
wwwroot/css/components/toast-notification.css
```

Verify the file exists and is being served.

## 🧪 Manual Test Cases

### Test Case 1: Success Toast
**Steps:**
1. Go to Calendar View
2. Click "New Event"
3. Fill in required fields
4. Click "Save"

**Expected:**
- Green toast appears top-right
- Message: "Event created successfully!"
- Auto-dismisses after 3 seconds

### Test Case 2: Error Toast
**Steps:**
1. Try to save event with empty title

**Expected:**
- Red toast appears top-right
- Message: "Failed to create event."
- Auto-dismisses after 5 seconds

### Test Case 3: SignalR Real-time Toast
**Steps:**
1. Open two browsers
2. Login as different users
3. User 1: Create event
4. Watch User 2's screen

**Expected:**
- User 2 sees green toast immediately
- Message: "Event 'EventName' created!"

## 🔍 Debug Mode

### Enable Detailed Logging

Add to `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.SignalR": "Debug",
      "Microsoft.AspNetCore.Http.Connections": "Debug"
    }
  }
}
```

### Check Blazor Component State

Add temporary logging to `CalendarView.razor`:
```csharp
private void ShowSuccess(string message)
{
    Logger.LogInformation("🔔 ShowSuccess called: {Message}", message);
    successMessage = message;
    StateHasChanged(); // Force UI update
    _ = Task.Delay(3000).ContinueWith(_ => {
        successMessage = null;
        InvokeAsync(StateHasChanged);
    });
}
```

## 🚨 Common Issues & Solutions

### Issue 1: Toast Appears but Disappears Immediately
**Cause:** StateHasChanged not being called
**Solution:** Already handled in the code with `InvokeAsync(StateHasChanged)`

### Issue 2: Toast Doesn't Auto-dismiss
**Cause:** Timer in ToastNotification component might not be working
**Solution:** Check ToastNotification component's timer logic

### Issue 3: Multiple Toasts Stack Up
**Cause:** Previous toasts not clearing
**Solution:** Each toast is independent with its own OnClose callback

### Issue 4: CSS Not Applied
**Cause:** CSS file not loaded
**Solution:** 
1. Check if `wwwroot/css/components/toast-notification.css` exists
2. Clear browser cache (Ctrl+Shift+Delete)
3. Hard refresh (Ctrl+F5)

### Issue 5: SignalR Toasts Don't Work
**Cause:** SignalR not connected
**Solution:** 
1. Check connection indicator in UI
2. Verify API is running on port 5006
3. Check browser console for WebSocket errors
4. Verify JWT token is valid

## 📋 File Dependencies

Toast notifications depend on:

```
CalendarView.razor
├── ToastNotification.razor (Component)
├── css/components/toast-notification.css (Styles)
├── Bootstrap Icons (bi-check-circle-fill, etc.)
└── SignalR Connection (for real-time)
```

## 🔄 Hot Reload Issues

If changes don't appear:
1. **Stop** both servers (Ctrl+C in both terminals)
2. **Clean** solution:
   ```powershell
   dotnet clean
   ```
3. **Restart** servers:
   ```powershell
   # Terminal 1
   cd EventScheduler.Api
   dotnet run
   
   # Terminal 2
   cd EventScheduler.Web
   dotnet run
   ```

## 📞 Still Not Working?

### Last Resort Checklist:
- [ ] API running on http://localhost:5006
- [ ] Web running on http://localhost:5292
- [ ] Logged in as valid user
- [ ] Browser cache cleared
- [ ] No console errors
- [ ] SignalR connected (green indicator)
- [ ] ToastNotification.razor file exists
- [ ] CSS file exists and loaded

### Debug Output to Look For:

**In API logs:**
```
✅ SignalR: Client connected!
📢 SignalR: Broadcasting EventCreated to all clients...
✅ SignalR: EventCreated notification sent successfully
```

**In Web console (F12):**
```
SignalR: Connected (Connection ID: xxxxx)
```

## 💡 Pro Tips

1. **Always check browser console first** - Most issues show up there
2. **Use incognito mode** - Eliminates cache issues
3. **Test with different browsers** - Firefox, Chrome, Edge
4. **Check network tab** - Verify API calls are succeeding
5. **Use browser's Blazor debug mode** - See component state changes

---

**Last Updated:** October 17, 2025
**Status:** Toast notifications should be working after the fix
