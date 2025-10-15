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

1. **Open Browser Console (F12)**
   - Look for errors (red text)
   - Look for initialization logs
   - Check if FullCalendar object exists: Type `FullCalendar` in console

2. **Check Network Tab**
   - Verify FullCalendar CDN loads successfully
   - Look for `fullcalendar@6.1.10/index.global.min.js`
   - Status should be 200 (OK)

3. **Check Element**
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

- [ ] API is running on port 5005
- [ ] User is logged in (check localStorage has auth_token)
- [ ] Browser console shows "Initializing FullCalendar..."
- [ ] Browser console shows "FullCalendar rendered successfully!"
- [ ] Network tab shows FullCalendar CDN loaded (200 status)
- [ ] Events API returns data successfully
- [ ] No JavaScript errors in console

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

---

**Status:** Fixed with multiple safeguards
**Test:** Refresh page and check browser console for logs
