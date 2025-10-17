# Offline Mode Enhancement - Server Down Detection

## 🎯 Overview

Enhanced the offline mode to detect **both** browser offline state AND server unreachability.

**Previous Behavior:**
- ✅ Worked when browser was set to "offline mode"
- ❌ Did NOT work when server was down/unreachable

**New Behavior:**
- ✅ Works when browser is offline
- ✅ **Works when server is down or unreachable**
- ✅ Continuous health monitoring
- ✅ Automatic reconnection when server returns

---

## 🔍 Detection Mechanisms

The system now monitors connectivity through **three channels**:

### 1. Browser Online/Offline Events
```javascript
window.addEventListener('online', handleBrowserOnline);
window.addEventListener('offline', handleBrowserOffline);
```

**Detects:** User's internet connection status

---

### 2. Blazor SignalR Connection State
```javascript
// Hooks into Blazor's reconnection handler
originalReconnectionHandler.onConnectionDown = function() {
    handleServerUnreachable(); // Treat as offline
};
```

**Detects:** WebSocket disconnections, server crashes

---

### 3. Periodic Server Health Checks
```javascript
setInterval(() => {
    checkServerHealth(); // Ping /health endpoint
}, 5000); // Every 5 seconds
```

**Detects:** 
- API server down
- Network issues
- Timeout (3 seconds)
- CORS errors
- Any fetch failures

---

## 🏥 Health Check Endpoint

### API Endpoint
```
GET http://localhost:5006/health
```

**Response (when healthy):**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-17T16:30:00.000Z"
}
```

**Timeout:** 3 seconds  
**Check Interval:** Every 5 seconds  
**Method:** Abort controller for clean timeout handling

---

## 📊 Offline Status Decision

The system considers you **OFFLINE** if **ANY** of these are true:

1. ❌ `navigator.onLine === false` (Browser offline)
2. ❌ Server health check fails
3. ❌ Blazor SignalR connection is down
4. ❌ Health endpoint doesn't respond within 3 seconds

The system considers you **ONLINE** only if **ALL** are true:

1. ✅ `navigator.onLine === true`
2. ✅ Server health check succeeds
3. ✅ Blazor SignalR connected (or reconnecting)

---

## 🔄 How It Works

### Scenario 1: Server Crashes
```
1. Server stops/crashes
2. Health check fails (3 second timeout)
3. JavaScript detects: isServerReachable = false
4. Notifies C#: UpdateNetworkStatus(false)
5. UI switches to offline mode
6. Events save to IndexedDB
7. Pending operations queue created
```

### Scenario 2: Server Restarts
```
1. Server comes back online
2. Next health check succeeds (within 5 seconds)
3. JavaScript detects: isServerReachable = true
4. Notifies C#: UpdateNetworkStatus(true)
5. UI switches to online mode
6. Pending operations auto-sync
7. IndexedDB updates from server
```

### Scenario 3: Network Disconnection
```
1. User loses internet
2. Browser fires 'offline' event
3. Health checks automatically stop
4. UI switches to offline mode immediately
5. When internet returns: 'online' event fires
6. Health check runs to verify server
7. If server reachable: UI goes online
```

---

## 🛠️ Files Modified

### 1. `network-status.js` (Major Enhancement)
**Changes:**
- ✅ Added server health check mechanism
- ✅ Added periodic polling (every 5 seconds)
- ✅ Hooks into Blazor reconnection handler
- ✅ Timeout handling (3 seconds)
- ✅ Combined browser + server status

**New Functions:**
```javascript
startServerHealthCheck()    // Starts 5-second polling
checkServerHealth()          // Pings /health endpoint
handleServerReachable()      // Server came back
handleServerUnreachable()    // Server went down
```

---

### 2. `NetworkStatusService.cs` (Enhanced)
**Changes:**
- ✅ Added IConfiguration injection
- ✅ Passes API URL to JavaScript
- ✅ Added ForceServerHealthCheckAsync() method
- ✅ Better logging

**New Constructor:**
```csharp
public NetworkStatusService(
    IJSRuntime jsRuntime, 
    ILogger<NetworkStatusService> logger,
    IConfiguration configuration) // NEW
```

---

### 3. `Program.cs` (API) - Health Endpoint
**Changes:**
- ✅ Added `/health` endpoint
- ✅ Returns JSON with timestamp

**New Code:**
```csharp
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}));
```

---

## 🧪 Testing Instructions

### Test 1: Server Down
```bash
# 1. Stop the API server (Ctrl+C in API terminal)
# 2. Wait 5 seconds
# Expected: Console shows "[NetworkStatus] Server is unreachable"
# Expected: Offline indicator appears in UI
# Expected: Events save to IndexedDB

# 3. Restart API server
# Expected: Within 5 seconds, "[NetworkStatus] Server is now reachable"
# Expected: Online indicator appears
# Expected: Pending operations sync automatically
```

### Test 2: Browser Offline Mode
```bash
# 1. Open DevTools → Network tab
# 2. Set throttling to "Offline"
# Expected: Immediate offline detection
# Expected: "[NetworkStatus] Browser offline event detected"

# 3. Set throttling back to "No throttling"
# Expected: "[NetworkStatus] Browser online event detected"
# Expected: Server health check runs
# Expected: If server is up, goes online
```

### Test 3: Blazor Connection Loss
```bash
# 1. Server running normally
# 2. Firewall blocks port 5292 (or kill Web server)
# Expected: Blazor connection drops
# Expected: "[NetworkStatus] Blazor connection down - treating as offline"
# Expected: Offline mode activates

# 3. Restore connection
# Expected: "[NetworkStatus] Blazor connection restored"
# Expected: Online mode resumes
```

### Test 4: Slow Network
```bash
# 1. DevTools → Network → Throttling → "Slow 3G"
# 2. If health check takes > 3 seconds, should fail
# Expected: Timeout after 3 seconds
# Expected: Offline mode activated
# Expected: Retries every 5 seconds
```

---

## 📋 Console Log Examples

### Successful Health Check
```
[NetworkStatus] Monitor initialized - checking both browser and server connectivity
[NetworkStatus] Server is now reachable
[NetworkStatus] Status: ONLINE
```

### Server Goes Down
```
[NetworkStatus] Server health check failed: TypeError: Failed to fetch
[NetworkStatus] Server is unreachable
[NetworkStatus] Status: OFFLINE
```

### Server Comes Back
```
[NetworkStatus] Server is now reachable
[NetworkStatus] Status: ONLINE
```

### Blazor Connection Events
```
[NetworkStatus] Blazor connection down - treating as offline
[NetworkStatus] Status: OFFLINE
...
[NetworkStatus] Blazor connection restored - treating as online
[NetworkStatus] Server is now reachable
[NetworkStatus] Status: ONLINE
```

---

## ⚙️ Configuration

### Timings (Adjustable in `network-status.js`)

```javascript
// Health check interval
serverCheckInterval = setInterval(..., 5000); // 5 seconds

// Health check timeout
setTimeout(() => controller.abort(), 3000); // 3 second timeout
```

**Recommended Values:**
- **Check Interval:** 5-10 seconds (balance between responsiveness and load)
- **Timeout:** 3-5 seconds (fast enough to detect issues, not too aggressive)

---

## 🎯 Benefits

1. ✅ **Automatic Detection** - No user action needed
2. ✅ **Multiple Failsafes** - Browser, Blazor, and health checks
3. ✅ **Fast Recovery** - Detects server return within 5 seconds
4. ✅ **Seamless UX** - Users don't notice the switch
5. ✅ **Data Safety** - All changes saved to IndexedDB
6. ✅ **Auto-Sync** - Pending operations sync when back online

---

## 🚀 Next Steps

1. **Restart Both Servers** (API and Web need rebuild for health endpoint)
2. **Hard Refresh Browser** (`Ctrl + Shift + R`)
3. **Test Server Down Scenario**:
   - Stop API server
   - Watch console for offline detection
   - Create/edit events (should save to IndexedDB)
   - Restart API server
   - Watch auto-sync happen

---

## 🐛 Troubleshooting

### "Health check not running"
**Check:**
- Is `network-status.js` loaded?
- Is `NetworkStatusService.InitializeAsync()` called?
- Console should show: "Monitor initialized - checking both browser and server connectivity"

### "Still shows online when server is down"
**Check:**
- Wait 5 seconds (first health check)
- Check if `/health` endpoint exists on API
- Look for CORS issues in console
- Verify API URL in configuration

### "Offline mode not activating"
**Check:**
- Console logs from `[NetworkStatus]`
- Browser console for JavaScript errors
- Ensure all three detection mechanisms are active

---

## 📚 Related Documentation

- `OFFLINE_MODE.md` - Complete offline mode guide
- `OFFLINE_TESTING_GUIDE.md` - Testing procedures
- `ERROR_ANALYSIS_AND_FIXES.md` - Recent fixes

---

**Status:** ✅ **Enhanced and Ready**  
**Date:** October 17, 2025  
**Feature:** Server-Down Detection + Browser Offline Detection
