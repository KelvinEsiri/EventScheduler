# FullCalendar Troubleshooting Guide

## Issue: Calendar Not Displaying

### Symptoms:
- Page loads but calendar doesn't appear
- Blank white space where calendar should be
- Only see "Interactive Calendar" header

### Fixes Applied:

#### 1. **Removed `firstRender` check from calendar initialization**
```csharp
// BEFORE (restrictive):
if (firstRender && hasCheckedAuth && !calendarInitialized && !isLoading)

// AFTER (more flexible):
if (hasCheckedAuth && !calendarInitialized && !isLoading)
```
**Why:** Calendar might need to initialize on subsequent renders if events load slowly.

#### 2. **Added StateHasChanged() after LoadEvents**
```csharp
await LoadEvents();
StateHasChanged();  // Force re-render after events loaded
```
**Why:** Ensures Blazor updates the UI after async event loading.

#### 3. **Increased initialization delay**
```csharp
await Task.Delay(200);  // Increased from 100ms
```
**Why:** Gives more time for FullCalendar library to fully load from CDN.

#### 4. **Added comprehensive logging**
```javascript
console.log('Initializing FullCalendar...', { elementId, eventCount: events.length });
console.log('Calendar element found:', calendarEl);
console.log('FullCalendar rendered successfully!');
```
**Why:** Helps diagnose issues in browser console.

#### 5. **Added FullCalendar library check**
```javascript
if (typeof FullCalendar === 'undefined') {
    console.error('FullCalendar library not loaded!');
    return false;
}
```
**Why:** Detects if CDN failed to load the library.

### How to Diagnose:

1. **Check Application Logs First**
   ```bash
   # PowerShell - Check authentication
   Get-Content .\EventScheduler.Web\logs\*.log | Select-String "CalendarView.*authenticated"
   
   # Check event loading
   Get-Content .\EventScheduler.Web\logs\*.log | Select-String "CalendarView.*Loaded.*events"
   
   # Check calendar initialization
   Get-Content .\EventScheduler.Web\logs\*.log | Select-String "CalendarView.*calendar initialization"
   
   # Check for errors
   Get-Content .\EventScheduler.Web\logs\*.log | Select-String "\[ERR\].*CalendarView"
   ```

2. **Open Browser Console (F12)**
   - Look for errors (red text)
   - Look for initialization logs
   - Check if FullCalendar object exists: Type `FullCalendar` in console

3. **Check Network Tab**
   - Verify FullCalendar CDN loads successfully
   - Look for `fullcalendar@6.1.10/index.global.min.js`
   - Status should be 200 (OK)
   - Check API call: `http://localhost:5006/api/events` - Status 200

4. **Check Element**
   - In Elements/Inspector tab, look for `<div id="calendar"></div>`
   - Should have FullCalendar classes like `.fc`, `.fc-view`, etc.

### Common Issues & Solutions:

#### Issue: "Calendar element not found"
**Solution:** Element ID mismatch
```html
<!-- Make sure this exists in your Razor markup: -->
<div id="calendar"></div>
```

#### Issue: "FullCalendar library not loaded"
**Solutions:**
1. Check internet connection (CDN needs internet)
2. Try alternative CDN:
   ```html
   <script src='https://unpkg.com/fullcalendar@6.1.10/index.global.min.js'></script>
   ```
3. Download library locally instead of CDN

#### Issue: Calendar shows but events don't appear
**Solution:** Check API connection
```csharp
// In LoadEvents() catch block, check Console output
Console.WriteLine($"Error loading events: {ex.Message}");
```

#### Issue: "Failed to initialize calendar"
**Solutions:**
1. Refresh the page (Ctrl+F5)
2. Clear browser cache
3. Check if multiple calendars initializing (conflict)
4. Increase delay: `await Task.Delay(500);`

### Verification Checklist:

- [ ] API is running on port 5006
- [ ] Web is running on port 5292
- [ ] User is logged in (check localStorage has auth_token)
- [ ] Browser console shows "Initializing FullCalendar..."
- [ ] Browser console shows "FullCalendar rendered successfully!"
- [ ] Network tab shows FullCalendar CDN loaded (200 status)
- [ ] Events API returns data successfully
- [ ] No JavaScript errors in console
- [ ] Application logs show successful authentication
- [ ] Application logs show events loaded
- [ ] Application logs show calendar initialized

### If Still Not Working:

1. **Try the simple list view:**
   Navigate to `/calendar` to verify events load correctly

2. **Check if it's a CDN issue:**
   ```html
   <!-- In App.razor, replace CDN with local files -->
   <!-- Download FullCalendar and host it locally -->
   ```

3. **Verify Blazor render mode:**
   CalendarView.razor should have:
   ```csharp
   @rendermode InteractiveServer
   ```

4. **Check for conflicting CSS:**
   Some Bootstrap styles might hide calendar elements

5. **Last resort - reload page:**
   Sometimes Blazor's hot reload causes issues during development

### Debug Console Commands:

Open browser console and try:
```javascript
// Check if FullCalendar loaded
typeof FullCalendar

// Check if calendar initialized
window.fullCalendarInterop.calendar

// Check if element exists
document.getElementById('calendar')

// Manually initialize (for testing)
window.fullCalendarInterop.initialize('calendar', null, [], true)
```

### Performance Notes:

- Calendar initialization takes 200-500ms
- First load from CDN takes 1-2 seconds
- Subsequent loads are cached (instant)
- Each event adds ~1ms to render time

### Comprehensive Logging (NEW)

The application now includes detailed Serilog logging for easier troubleshooting:

**Log Locations:**
- **API**: `EventScheduler.Api/logs/eventscheduler-api-<date>.log`
- **Web**: `EventScheduler.Web/logs/eventscheduler-web-<date>.log`

**Key Log Messages to Look For:**

✅ **Successful Flow:**
```
[15:30:45 INF] CalendarView: OnInitializedAsync started
[15:30:45 INF] CalendarView: User authenticated in OnInitializedAsync
[15:30:45 INF] CalendarView: API token set from auth state
[15:30:45 INF] CalendarView: Loading events from API...
[15:30:45 INF] CalendarView: Loaded 5 events successfully
[15:30:45 INF] CalendarView: Starting calendar initialization - Event count: 5
[15:30:46 INF] CalendarView: Calling fullCalendarInterop.initialize with 5 events
[15:30:46 INF] CalendarView: ✅ Calendar initialized successfully!
```

❌ **Error Patterns:**
```
[15:30:45 WRN] CalendarView: User not authenticated, redirecting to login
[15:30:45 ERR] CalendarView: Failed to load events from API
[15:30:46 ERR] CalendarView: ❌ Calendar initialization returned false
[15:30:46 ERR] CalendarView: ❌ Exception during calendar initialization
```

**See Also:**
- [Logging Guide](LOGGING_GUIDE.md) - Complete logging documentation
- [Serilog Implementation](../SERILOG_LOGGING_IMPLEMENTATION.md) - Configuration details

---

**Status:** Fixed with multiple safeguards + comprehensive logging
**Last Updated:** October 15, 2025
**Test:** Refresh page and check both browser console AND application logs
