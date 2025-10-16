# 🚀 QUICK START - SignalR Fix Applied

## ⚡ 3 Simple Steps to Get Real-Time Updates Working

### Step 1: Restart API Server
Open a terminal and run:
```powershell
cd "c:\Users\HP PC\Desktop\Projects\EventScheduler\EventScheduler.Api"
dotnet run
```
✅ Wait for: **"EventScheduler API listening on http://localhost:5005"**

### Step 2: Restart Web Server
Open a NEW terminal and run:
```powershell
cd "c:\Users\HP PC\Desktop\Projects\EventScheduler\EventScheduler.Web"
dotnet run
```
✅ Wait for: **"EventScheduler Web listening on http://localhost:5292"**

### Step 3: Test It!
1. Go to: `http://localhost:5292/calendar-view`
2. Login
3. Look at the top of the page

**✅ SUCCESS = Green bar:** "Connected to real-time updates"
**❌ FAIL = Red bar:** "Real-time updates unavailable"

---

## 🎯 Test Real-Time Feature

### Quick Test:
1. Open calendar in 2 different browsers (Chrome + Edge)
2. Login to both
3. Create an event in one browser
4. **Watch it appear instantly in the other!**

---

## ❌ If Still Not Working

Run the batch file instead:
```
Double-click: restart-servers.bat
```

Then try the test again.

---

## 📋 What Was Fixed

1. ✅ Hub URL corrected (`/eventHub` → `/hubs/events`)
2. ✅ API port fixed (5006 → 5005)
3. ✅ JWT authentication added
4. ✅ SignalR broadcasting enabled
5. ✅ Required packages installed

---

## 🔍 Quick Checks

### Is API Running?
Open: `http://localhost:5005`
Should see: "EventScheduler API is running!"

### Is Web Running?
Open: `http://localhost:5292`
Should see: Login page

### Is SignalR Working?
1. Login to calendar
2. Press F12 → Console tab
3. Should see: "SignalR: Connected successfully"

---

**That's it!** Real-time updates should now work. 🎉

**Need more help?** Check: `SIGNALR_FIX_COMPLETE.md`
