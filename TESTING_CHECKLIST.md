# Testing Checklist - Serilog Implementation

## Pre-Test Setup ✅

- [x] All packages restored
- [x] Build succeeded with no errors
- [x] Documentation updated
- [x] Code changes complete

## Test Execution

### 1. Build Verification
```bash
cd "c:\Users\HP PC\Desktop\Projects\EventScheduler"
dotnet build
```

**Expected Result**:
- [ ] ✅ Build succeeded
- [ ] No compilation errors
- [ ] All projects build successfully

**If Failed**: Check error messages and verify package versions

---

### 2. Start Applications
```bash
.\run-all.bat
```

**Expected Console Output - API**:
- [ ] `========================================`
- [ ] `Starting EventScheduler API`
- [ ] `Database migration completed successfully`
- [ ] `Database contains X users and Y events`
- [ ] `EventScheduler API listening on http://localhost:5006`
- [ ] `API is ready to handle requests`

**Expected Console Output - Web**:
- [ ] `========================================`
- [ ] `Starting EventScheduler Web Application`
- [ ] `EventScheduler Web listening on http://localhost:5292`
- [ ] `Connected to API at: http://localhost:5006`
- [ ] `Web application is ready`

**If Failed**: Check for port conflicts or database connection issues

---

### 3. Test Login Flow

**Action**: Navigate to `http://localhost:5292` and login

**Expected Console Logs**:
- [ ] See HTTP request logging from Serilog
- [ ] No error messages during login

**Expected File Logs**:
```bash
Get-Content .\EventScheduler.Web\logs\eventscheduler-web-*.log -Tail 20
```
- [ ] Login-related API calls visible

**If Failed**: Check API logs for authentication errors

---

### 4. Test Calendar View

**Action**: After login, should auto-redirect to `/calendar-view`

**Expected Console Logs (Web)**:
```
[HH:mm:ss INF] CalendarView: OnInitializedAsync started
[HH:mm:ss INF] CalendarView: User authenticated in OnInitializedAsync
[HH:mm:ss INF] CalendarView: API token set from auth state
[HH:mm:ss INF] CalendarView: Loading events from API...
[HH:mm:ss INF] CalendarView: Loaded X events successfully
[HH:mm:ss INF] CalendarView: LoadEvents completed, isLoading set to false
[HH:mm:ss INF] CalendarView: OnAfterRenderAsync - FirstRender=False, HasCheckedAuth=True, CalendarInitialized=False, IsLoading=False
[HH:mm:ss INF] CalendarView: Starting calendar initialization - Event count: X
[HH:mm:ss INF] CalendarView: Calling fullCalendarInterop.initialize with X events
[HH:mm:ss INF] CalendarView: ✅ Calendar initialized successfully!
```

**Browser Console (F12)**:
- [ ] `FullCalendar library loaded successfully`
- [ ] `Element found: calendar`
- [ ] `Initializing FullCalendar...`
- [ ] `FullCalendar initialized successfully with X events`
- [ ] `FullCalendar rendered`
- [ ] No JavaScript errors (red text)

**Visual Check**:
- [ ] Calendar displays on page
- [ ] Month/Week/Day/List view buttons visible
- [ ] Events appear if any exist
- [ ] No error messages on page

**If Failed**: 
1. Check browser console for JavaScript errors
2. Check application logs for C# errors
3. Follow `docs/CALENDAR_TROUBLESHOOTING.md`

---

### 5. Test Event Creation

**Action**: Click "New Event" button or click a date on calendar

**Expected Console Logs**:
```
[HH:mm:ss INF] CalendarView: Saving event - IsEditMode=False, EventId=0, Title=Test Event
[HH:mm:ss INF] CalendarView: New event created successfully - Title=Test Event
[HH:mm:ss INF] CalendarView: Loading events from API...
[HH:mm:ss INF] CalendarView: Loaded X events successfully
```

**Expected API Logs**:
```bash
Get-Content .\EventScheduler.Api\logs\eventscheduler-api-*.log -Tail 20
```
- [ ] POST /api/events request logged
- [ ] Event created successfully

**Visual Check**:
- [ ] Modal opens for event details
- [ ] Event appears on calendar after save
- [ ] Success message displayed

**If Failed**: Check both Web and API logs for error details

---

### 6. Test Event Drag & Drop

**Action**: Drag an event to a different date/time

**Expected Console Logs**:
```
[HH:mm:ss INF] CalendarView: Event X dropped - NewStart=YYYY-MM-DD HH:mm:ss, NewEnd=YYYY-MM-DD HH:mm:ss, AllDay=False
[HH:mm:ss INF] CalendarView: Event X rescheduled successfully
[HH:mm:ss INF] CalendarView: Loading events from API...
[HH:mm:ss INF] CalendarView: Loaded X events successfully
```

**Visual Check**:
- [ ] Event moves to new position
- [ ] Success message shown
- [ ] Event stays in new position after reload

**If Failed**: Check if editable=true in FullCalendar config

---

### 7. Test Event Resize

**Action**: Drag event edge to resize duration

**Expected Console Logs**:
```
[HH:mm:ss INF] CalendarView: Event X resized - NewStart=YYYY-MM-DD HH:mm:ss, NewEnd=YYYY-MM-DD HH:mm:ss
[HH:mm:ss INF] CalendarView: Event X duration updated successfully
[HH:mm:ss INF] CalendarView: Loading events from API...
[HH:mm:ss INF] CalendarView: Loaded X events successfully
```

**Visual Check**:
- [ ] Event duration changes visually
- [ ] Success message shown
- [ ] Duration persists after reload

**If Failed**: Check if eventResize callback is configured

---

### 8. Check Log Files

**Web Logs**:
```bash
Get-Content .\EventScheduler.Web\logs\eventscheduler-web-*.log
```

**Verify**:
- [ ] File created in logs directory
- [ ] Contains timestamped entries
- [ ] Shows all CalendarView operations
- [ ] Includes structured properties (EventCount, etc.)

**API Logs**:
```bash
Get-Content .\EventScheduler.Api\logs\eventscheduler-api-*.log
```

**Verify**:
- [ ] File created in logs directory
- [ ] Contains API request logs
- [ ] Shows database operations
- [ ] Includes startup statistics

---

### 9. Test Error Logging

**Action**: Stop API, try to load calendar

**Expected Console Logs**:
```
[HH:mm:ss ERR] CalendarView: Failed to load events from API
System.Net.Http.HttpRequestException: Connection refused
```

**Action**: Restart API, refresh page

**Verify**:
- [ ] Error logged with full stack trace
- [ ] User sees error message
- [ ] Application recovers when API restarts

---

### 10. Log Filtering Tests

**Filter by Component**:
```bash
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "CalendarView"
```
- [ ] Returns only CalendarView logs

**Filter by Errors**:
```bash
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "\[ERR\]"
```
- [ ] Returns only error entries

**Filter by Event Operations**:
```bash
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "Saving event|dropped|resized"
```
- [ ] Returns event modification logs

---

## Summary

### Success Criteria

**All Green** = Ready for Production ✅
- [ ] Build succeeds
- [ ] Both apps start without errors
- [ ] Login works and redirects
- [ ] Calendar displays correctly
- [ ] Events can be created
- [ ] Events can be dragged/dropped
- [ ] Events can be resized
- [ ] Logs are written to files
- [ ] Logs are searchable
- [ ] Errors are logged with details

### Issues Found

**Record any failures here**:
```
Issue #1: 
Description: 
Logs: 
Solution: 

Issue #2:
Description:
Logs:
Solution:
```

### Performance Check

**Log File Growth**:
- [ ] Web log size reasonable (< 5MB per day typical)
- [ ] API log size reasonable (< 10MB per day typical)
- [ ] Old logs cleaned up (> 14 days deleted)

**Application Performance**:
- [ ] Page loads quickly (< 2 seconds)
- [ ] Logging doesn't slow down operations
- [ ] UI remains responsive

---

## Next Steps

### If All Tests Pass ✅
1. Commit changes to Git
2. Deploy to staging/production (if applicable)
3. Monitor logs for any issues
4. Document any additional findings

### If Tests Fail ❌
1. Review logs for error details
2. Consult `docs/CALENDAR_TROUBLESHOOTING.md`
3. Check browser console (F12)
4. Verify all packages installed correctly
5. Try clean build: `dotnet clean && dotnet build`

---

**Testing Date**: _____________  
**Tested By**: _____________  
**Build Version**: .NET 9.0  
**Result**: [ ] ✅ Pass  [ ] ❌ Fail  
**Notes**: 

_______________________________________
_______________________________________
_______________________________________
