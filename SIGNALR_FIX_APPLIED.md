# SignalR Real-Time Updates - Fix Applied

## 🔧 Changes Made

### 1. **Fixed SignalR Hub URL**
- ✅ Changed from `/eventHub` to `/hubs/events` (matches API endpoint)
- ✅ Added proper token authentication for SignalR connection
- ✅ Connection now includes JWT token from authentication state

### 2. **Fixed API Base URL**
- ✅ Changed from `http://localhost:5006` to `http://localhost:5005` (correct API port)
- ✅ Updated both HttpClient and SignalR hub URLs

### 3. **Updated CORS Policy**
- ✅ Added additional allowed origins for flexibility
- ✅ Ensures SignalR WebSocket connections are allowed

### 4. **Added SignalR Broadcasting**
- ✅ Installed `Microsoft.AspNetCore.SignalR.Core` in Application layer
- ✅ Updated EventService to broadcast notifications on:
  - Event Created
  - Event Updated
  - Event Deleted
- ✅ Configured dependency injection to pass HubContext to EventService

---

## 🚀 How to Test

### Step 1: Stop Running Servers
If you have servers running, stop them first (Ctrl+C in terminals).

### Step 2: Start API Server
```powershell
cd "c:\Users\HP PC\Desktop\Projects\EventScheduler\EventScheduler.Api"
dotnet run
```

Wait for: `EventScheduler API listening on http://localhost:5005`

### Step 3: Start Web Server (New Terminal)
```powershell
cd "c:\Users\HP PC\Desktop\Projects\EventScheduler\EventScheduler.Web"
dotnet run
```

Wait for: `EventScheduler Web listening on http://localhost:5292`

### Step 4: Test Real-Time Updates
1. Open browser to `http://localhost:5292/calendar-view`
2. Login with your credentials
3. Look for connection status:
   - Should see: **"Connected to real-time updates"** (green)
   - Should NOT see: "Real-time updates unavailable" (red)

### Step 5: Test Broadcasting
**Test with TWO browser windows:**

1. Open `http://localhost:5292/calendar-view` in **Chrome**
2. Open `http://localhost:5292/calendar-view` in **Edge** (or incognito mode)
3. Login to both
4. Create/edit/delete an event in **one** browser
5. Watch it automatically update in **the other** browser (no refresh needed!)

---

## 🔍 Verification Checklist

### Connection Status
- [ ] Green "Connected to real-time updates" appears at top
- [ ] No red error message
- [ ] Calendar loads successfully

### Browser Console Check (F12)
Look for these logs:
```
SignalR: Connected successfully
Calendar initialized successfully
```

Should NOT see:
```
SignalR: Failed to connect
Failed to negotiate with the server
```

### API Console Check
Look for these logs when events are created:
```
[INFO] Client connected: <ConnectionId>
[INFO] Event X created successfully
[INFO] SignalR notification sent for event creation: X
```

### Real-Time Test
1. **Create Event** - Both browsers update immediately
2. **Edit Event** - Changes appear in both browsers
3. **Delete Event** - Removal syncs to both browsers
4. **Drag Event** - New position syncs automatically

---

## 🐛 If Still Not Working

### Check 1: Port Conflicts
```powershell
# Check if ports are in use
netstat -ano | findstr :5005
netstat -ano | findstr :5292
```

### Check 2: Firewall
- Windows Firewall might block SignalR WebSockets
- Allow dotnet.exe in firewall settings

### Check 3: Browser Console Errors
Press F12 and check Console tab for:
- WebSocket connection errors
- CORS errors
- 401 Unauthorized errors

### Check 4: API Logs
Check file: `EventScheduler.Api/logs/eventscheduler-api-<date>.log`
Look for:
```
Client connected: <id>
EventCreated notification sent
```

### Check 5: Web Logs
Check file: `EventScheduler.Web/logs/eventscheduler-web-<date>.log`
Look for:
```
SignalR: Connected successfully
```

---

## 📊 What Should Happen

### When You Create an Event:
1. **Your Browser:**
   - Modal closes
   - Green success message appears
   - Event appears on calendar immediately

2. **Other Browsers (same user):**
   - Event appears automatically
   - No refresh needed
   - Green notification: "Event created successfully!"

3. **API Console:**
   ```
   [INFO] Event X created successfully
   [INFO] SignalR notification sent
   ```

4. **Web Console (F12):**
   ```
   SignalR: Event created - <message>
   Loading events from API...
   Loaded X events successfully
   ```

---

## 🔧 Technical Details

### SignalR Flow:
```
User Action (Create Event)
    ↓
API: EventService.CreateEventAsync()
    ↓
Database: Event Saved
    ↓
SignalR: _hubContext.Clients.All.SendAsync("EventCreated", message)
    ↓
All Connected Clients Receive Message
    ↓
CalendarView: OnInitializedAsync → Handler
    ↓
LoadEvents() → Calendar Refresh
```

### Connection Details:
- **Protocol:** WebSockets (fallback to Server-Sent Events)
- **Endpoint:** `http://localhost:5005/hubs/events`
- **Authentication:** JWT Bearer token in query string
- **Reconnect:** Automatic with exponential backoff

---

## 💡 Quick Fixes

### Fix 1: Clear Browser Cache
- Press `Ctrl+Shift+Delete`
- Clear cache and cookies
- Restart browser

### Fix 2: Restart Both Servers
```powershell
# Terminal 1 - API
cd EventScheduler.Api
dotnet clean
dotnet build
dotnet run

# Terminal 2 - Web
cd EventScheduler.Web
dotnet clean
dotnet build
dotnet run
```

### Fix 3: Check Authentication
- Logout and login again
- Token might have expired
- Check if JWT is in user claims

---

## ✅ Success Indicators

You'll know it's working when:
1. ✅ Green "Connected to real-time updates" message
2. ✅ Events sync across multiple browser windows
3. ✅ No page refresh needed for updates
4. ✅ API logs show "SignalR notification sent"
5. ✅ Web console shows "SignalR: Connected successfully"

---

## 📞 Still Having Issues?

If real-time updates still don't work:

1. Check API is running on port 5005
2. Check Web is running on port 5292
3. Check both applications show "ready" in console
4. Try using different browser
5. Check Windows Firewall settings
6. Verify no antivirus blocking WebSockets

---

**Status:** ✅ **Fix Applied - Ready to Test**
**Date:** October 15, 2025
**Changes:** CalendarView.razor, Program.cs (API & Web), EventService.cs
