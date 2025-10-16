# 🔍 SignalR Debugging Guide - WITH DETAILED LOGGING

## 🎯 What Was Added

### Comprehensive Serilog Logging
Added detailed logging at every step to track exactly what's happening:

**Web Application (CalendarView.razor):**
- ✅ Authentication status
- ✅ JWT token presence and length
- ✅ SignalR connection initialization
- ✅ Hub URL being used
- ✅ Connection success/failure with detailed error types
- ✅ Event handler registration
- ✅ Message receipt confirmation
- ✅ Reconnection attempts

**API Application:**
- ✅ SignalR configuration
- ✅ Hub endpoint mapping
- ✅ Client connection tracking
- ✅ Client disconnection tracking
- ✅ Broadcast message sending
- ✅ HubContext availability

---

## 🚀 RESTART & CHECK LOGS

### Step 1: Stop ALL Running Servers
Make sure no servers are running. Kill all terminals or run:
```powershell
taskkill /F /IM EventScheduler.Api.exe 2>nul
taskkill /F /IM EventScheduler.Web.exe 2>nul
```

### Step 2: Start API Server (Terminal 1)
```powershell
cd "c:\Users\HP PC\Desktop\Projects\EventScheduler\EventScheduler.Api"
dotnet run
```

**WATCH FOR THESE LOGS:**
```
========================================
Starting EventScheduler API
========================================
Configuring SignalR...
✅ SignalR configured successfully
Mapping SignalR hub to /hubs/events...
✅ SignalR hub endpoint configured at: /hubs/events
========================================
🚀 EventScheduler API is READY
========================================
API listening on: http://localhost:5005
SignalR Hub at: http://localhost:5005/hubs/events
✅ API is ready to accept requests
```

❌ **If you DON'T see these logs, there's a problem with the API!**

### Step 3: Start Web Server (Terminal 2)
```powershell
cd "c:\Users\HP PC\Desktop\Projects\EventScheduler\EventScheduler.Web"
dotnet run
```

**WATCH FOR THESE LOGS:**
```
========================================
Starting EventScheduler Web Application
========================================
EventScheduler Web listening on http://localhost:5292
```

### Step 4: Open Browser & Check Logs

1. Open browser to: `http://localhost:5292/calendar-view`
2. Login
3. **IMMEDIATELY check BOTH terminal windows for logs**

---

## 📋 EXPECTED LOG SEQUENCE

### When Page Loads - Web Terminal Should Show:

```
========================================
CalendarView: OnInitializedAsync started
========================================
CalendarView: User authenticated = True
CalendarView: ✅ User authenticated in OnInitializedAsync
CalendarView: ✅ API token set (length: 500+)
CalendarView: About to initialize SignalR...
=== InitializeSignalR: Starting SignalR initialization ===
InitializeSignalR: API Base URL = http://localhost:5005
InitializeSignalR: JWT token found (length: 500+)
InitializeSignalR: Connecting to hub at: http://localhost:5005/hubs/events
InitializeSignalR: Hub connection builder configured
InitializeSignalR: Registering event handlers
InitializeSignalR: Starting hub connection...
SignalR: ✅✅✅ Successfully connected! Connection ID: <some-guid>
SignalR: Connection State: Connected
```

### When Page Loads - API Terminal Should Show:

```
========================================
✅ SignalR: Client connected!
Connection ID: <some-guid>
User: <username or Anonymous>
========================================
```

---

## 🔍 WHAT TO LOOK FOR

### ✅ SUCCESS INDICATORS

**Web Logs:**
- "JWT token found"
- "Successfully connected!"
- "Connection State: Connected"

**API Logs:**
- "Client connected!"
- Connection ID displayed

**Browser:**
- Green bar: "Connected to real-time updates"

### ❌ FAILURE INDICATORS & SOLUTIONS

#### Problem 1: "No JWT token found!"
**Web Log:**
```
InitializeSignalR: No JWT token found! SignalR connection will fail.
```

**Solution:**
- Logout and login again
- Token might not be in claims
- Check AuthStateProvider

#### Problem 2: "HTTP connection failed"
**Web Log:**
```
SignalR: ❌ HTTP connection failed - Could not reach API server
```

**Solution:**
- API is not running
- Check API is on port 5005
- Try: `curl http://localhost:5005`

#### Problem 3: "Authentication failed"
**Web Log:**
```
SignalR: ❌ Authentication failed - Invalid or expired token
```

**Solution:**
- Token is invalid or expired
- Logout and login again
- Check JWT configuration matches

#### Problem 4: No "Client connected" in API
**API shows NO connection log**

**Possible Causes:**
- CORS blocking connection
- Hub endpoint not mapped correctly
- Port conflict

**Solution:**
- Check API logs for "SignalR hub endpoint configured"
- Verify no errors during startup
- Try different browser

#### Problem 5: Connected but no updates
**Web shows "Connected" but events don't sync**

**Check:**
- API logs when creating event
- Should see: "📢 SignalR: Broadcasting EventCreated"
- Should see: "✅ SignalR: EventCreated notification sent"

**If NOT visible:**
- HubContext is null
- Check dependency injection in Program.cs

---

## 🧪 TESTING BROADCAST

### Create an Event:

**Web Terminal Should Show:**
```
SignalR: ✅ EventCreated notification received - Event 'Test' has been created
Loading events from API...
Loaded X events successfully
```

**API Terminal Should Show:**
```
Event X created successfully for user Y
📢 SignalR: Broadcasting EventCreated to all clients...
✅ SignalR: EventCreated notification sent successfully for event X
```

### If NO broadcast logs appear:

Check API logs for:
```
⚠️ SignalR: HubContext is null, cannot broadcast EventCreated
```

This means dependency injection failed!

---

## 📝 LOG FILES

If console logs are too fast, check log files:

**API Logs:**
```
EventScheduler.Api/logs/eventscheduler-api-<date>.log
```

**Web Logs:**
```
EventScheduler.Web/logs/eventscheduler-web-<date>.log
```

Open with:
```powershell
notepad "EventScheduler.Api\logs\eventscheduler-api-<today>.log"
notepad "EventScheduler.Web\logs\eventscheduler-web-<today>.log"
```

---

## 🎯 CHECKLIST

Before reporting issues, verify:

- [ ] API terminal shows "✅ SignalR hub endpoint configured"
- [ ] Web terminal shows "✅✅✅ Successfully connected!"
- [ ] API terminal shows "✅ SignalR: Client connected!"
- [ ] Browser shows green "Connected" message
- [ ] Creating event shows "📢 Broadcasting" in API logs
- [ ] Creating event shows "✅ notification received" in Web logs

---

## 📞 REPORT FORMAT

If still not working, provide:

1. **API Startup Logs** (first 50 lines)
2. **Web Startup Logs** (first 50 lines)
3. **API Connection Log** (when page loads)
4. **Web Connection Log** (when page loads)
5. **Screenshot of browser** (showing connection status)
6. **Browser Console** (F12 → Console tab)

---

**Date:** October 15, 2025
**Status:** ✅ Detailed logging added - Ready to debug
**Action:** Restart both servers and check logs above
