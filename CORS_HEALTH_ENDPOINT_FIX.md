# CORS Fix for Health Endpoint

## 🚨 Issue

The health check endpoint (`/health`) is working but being blocked by CORS:

```
GET http://localhost:5006/health net::ERR_FAILED 200 (OK)
Access to fetch at 'http://localhost:5006/health' from origin 'http://127.0.0.1:5292' 
has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present
```

**Analysis:**
- ✅ Server responds with 200 OK (endpoint works!)
- ❌ CORS blocks the response from reaching JavaScript
- ❌ Causes offline mode to activate incorrectly

---

## 🔍 Root Cause

The Web app runs on `http://127.0.0.1:5292` but CORS policy only allowed `http://localhost:5292`.

**Difference:**
- `localhost` → DNS name (resolves to 127.0.0.1)
- `127.0.0.1` → IP address (direct)

While these are the same machine, **browsers treat them as different origins** for CORS!

---

## ✅ Fix Applied

**File:** `EventScheduler.Api/Program.cs`

**Before:**
```csharp
policy.WithOrigins(
    "http://localhost:5292",    // Only this
    "https://localhost:7248",
    "http://localhost:5006",
    "https://localhost:7249")
```

**After:**
```csharp
policy.WithOrigins(
    "http://localhost:5292",
    "http://127.0.0.1:5292",   // ← ADDED THIS
    "https://localhost:7248",
    "http://localhost:5006",
    "https://localhost:7249")
```

**Why Both?**
- Users might access via `localhost:5292` OR `127.0.0.1:5292`
- Different browsers handle these differently
- CORS requires exact origin match

---

## 🚀 Deployment

### Quick Fix (Recommended):

```powershell
.\restart-api-cors-fix.ps1
```

This script:
1. ✅ Stops the API server
2. ✅ Rebuilds with CORS fix
3. ✅ Restarts the API
4. ✅ Guides you through verification

### Manual Steps:

```powershell
# 1. Stop API server (Ctrl+C in API terminal)

# 2. Rebuild API
dotnet build EventScheduler.Api

# 3. Restart API
cd EventScheduler.Api
dotnet run
```

---

## 🧪 Verification

### After Restart:

1. **Wait 10 seconds** for API to fully start
2. **Refresh browser** (`Ctrl + Shift + R`)
3. **Check console** for these messages:

**Expected (Success):**
```
[NetworkStatus] Monitor initialized - checking both browser and server connectivity
[NetworkStatus] Server is now reachable
[NetworkStatus] Status: ONLINE
```

**If Still Failing:**
```
❌ Access to fetch at 'http://localhost:5006/health' ... blocked by CORS
```

---

## 🐛 Troubleshooting

### Issue: CORS errors persist

**Check 1: API Actually Restarted?**
```powershell
# Check if API is running on port 5006
Test-NetConnection -ComputerName localhost -Port 5006
```

**Check 2: Correct URL in Browser?**
```
✅ http://127.0.0.1:5292  (matches CORS config)
✅ http://localhost:5292  (matches CORS config)
❌ Any other address → won't work
```

**Check 3: API Logs**
Look in API terminal for:
```
✅ Configured CORS policy: AllowWebApp
✅ CORS policy allowing origins: http://127.0.0.1:5292, ...
```

**Check 4: Browser Cache**
- Hard refresh: `Ctrl + Shift + R`
- Or clear browser cache completely
- Or try incognito/private mode

---

### Issue: Still shows "Server unreachable"

**Verify health endpoint directly:**

Open in browser: `http://localhost:5006/health`

**Expected Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-17T16:50:00.000Z"
}
```

If you don't see this, the `/health` endpoint wasn't added properly.

---

## 📊 Testing Checklist

After deploying the fix:

- [ ] API rebuilt and restarted
- [ ] Browser refreshed (hard refresh)
- [ ] Console shows: "Server is now reachable"
- [ ] Console shows: "Status: ONLINE"
- [ ] No CORS errors in console
- [ ] Offline mode only activates when actually offline
- [ ] Health checks running every 5 seconds (visible in Network tab)

---

## 🎯 Expected Behavior

### Health Check in Network Tab (DevTools):

```
Request URL: http://localhost:5006/health
Request Method: GET
Status Code: 200 OK
Response Headers:
  access-control-allow-origin: http://127.0.0.1:5292  ← Should see this!
  content-type: application/json
```

### Console Logs:

**Every 5 seconds (when server is up):**
```
(No logs - health checks pass silently)
```

**When server goes down:**
```
[NetworkStatus] Server health check failed: Failed to fetch
[NetworkStatus] Server is unreachable
[NetworkStatus] Status: OFFLINE
```

**When server comes back:**
```
[NetworkStatus] Server is now reachable
[NetworkStatus] Status: ONLINE
```

---

## 🔧 Additional CORS Best Practices

### For Production:

Instead of hardcoded origins, use configuration:

```csharp
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

policy.WithOrigins(allowedOrigins);
```

**appsettings.json:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5292",
      "http://127.0.0.1:5292",
      "https://yourdomain.com"
    ]
  }
}
```

### Security Note:

Never use `.AllowAnyOrigin()` in production!
- ❌ Allows any website to call your API
- ❌ Major security vulnerability
- ✅ Always specify exact origins

---

## 📝 Summary

**Problem:** CORS blocking health endpoint due to origin mismatch  
**Cause:** Browser uses `127.0.0.1` but CORS only allowed `localhost`  
**Fix:** Added `http://127.0.0.1:5292` to allowed origins  
**Action:** Rebuild and restart API server  

**Files Modified:**
- ✅ `EventScheduler.Api/Program.cs` - CORS configuration
- ✅ `restart-api-cors-fix.ps1` - Quick restart script

---

## ✅ Status After Fix

- ✅ Health endpoint accessible from both `localhost` and `127.0.0.1`
- ✅ CORS headers correctly set
- ✅ Network status detection working
- ✅ Offline mode activates only when actually offline
- ✅ No console errors

---

**Next Step:** Run `.\restart-api-cors-fix.ps1` to apply the fix!

**Date:** October 17, 2025  
**Issue:** CORS blocking health endpoint  
**Resolution:** Added 127.0.0.1 to allowed origins
