# 🔧 Quick Fix Guide - IndexedDB Error

## ⚠️ You're Seeing This Error:
```
DataError: Failed to execute 'put' on 'IDBObjectStore': 
Evaluating the object store's key path did not yield a value.
```

## ✅ Solution (3 Steps):

### Step 1: Stop Your Servers
Press `Ctrl+C` in both terminal windows (API and Web)

---

### Step 2: Run the Fix Script

Open PowerShell in the project root and run:

```powershell
.\fix-and-restart.ps1
```

**What this does:**
- ✅ Stops all processes
- ✅ Cleans build artifacts  
- ✅ Rebuilds with the fixes
- ✅ Restarts both servers

---

### Step 3: Clear Browser Cache

After servers restart, open your browser:

1. Press `F12` to open DevTools
2. Go to **Console** tab
3. Paste this and press Enter:

```javascript
indexedDB.deleteDatabase('EventSchedulerOfflineDB');
localStorage.clear();
location.reload();
```

---

## ✅ Expected Result

After reload, you should see in console:

```
[OfflineStorage] Attempting to save 4 events
[OfflineStorage] Sample event structure: { hasLowercaseId: true, ... }
[OfflineStorage] Successfully saved 4 events, skipped 0
```

**No more DataError!** 🎉

---

## 🐛 Still Not Working?

### Checklist:
- [ ] Did you rebuild? (Not just save files)
- [ ] Did you restart BOTH servers?
- [ ] Did you clear browser cache?
- [ ] Did you hard-refresh the page? (Ctrl+Shift+R)

### Manual Rebuild:
```powershell
# Stop servers first!
dotnet clean
dotnet build EventScheduler.sln

# Then restart manually:
# Terminal 1:
cd EventScheduler.Api
dotnet run

# Terminal 2:  
cd EventScheduler.Web
dotnet run
```

### Check the Fixes Were Applied:

1. **C# File Check:**
   Open: `EventScheduler.Web\Services\OfflineStorageService.cs`
   
   Should contain (around line 19-23):
   ```csharp
   private static readonly JsonSerializerOptions JsonOptions = new()
   {
       PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
       WriteIndented = false
   };
   ```

2. **JavaScript File Check:**
   Open: `EventScheduler.Web\wwwroot\js\offline-storage.js`
   
   Should contain (around line 66-74):
   ```javascript
   if (!event.id && event.Id) {
       event.id = event.Id;
       console.log(`[OfflineStorage] Converted Id to id for event: ${event.id}`);
   }
   ```

---

## 📝 What Was Fixed?

**Problem:** C# sends `Id` (uppercase), but JavaScript/IndexedDB expects `id` (lowercase)

**Solution:**
1. **C# Side:** Serialize with camelCase naming policy
2. **JavaScript Side:** Fallback converter for backward compatibility
3. **Logging:** Added diagnostic messages to track the issue

---

## 🆘 Need More Help?

See full documentation: `ERROR_ANALYSIS_AND_FIXES.md`

**The key is:** You MUST rebuild and restart - just saving files isn't enough!
