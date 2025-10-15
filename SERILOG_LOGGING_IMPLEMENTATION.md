# Serilog Logging Implementation Summary

## Overview
Comprehensive Serilog logging has been added to both the API and Web projects to enable better error tracing and diagnostics, especially for troubleshooting the calendar initialization issue.

## What Was Added

### 1. **Serilog Packages** (EventScheduler.Web.csproj)
```xml
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
```

**Note**: Upgraded to version 9.0.0 for better compatibility with .NET 9.0

### 2. **API Configuration** (EventScheduler.Api/Program.cs)
- **Log Location**: `logs/eventscheduler-api-.log`
- **Rolling Interval**: Daily
- **Retention**: 14 days
- **File Size Limit**: 10MB per file
- **Output**: Console (timestamped) + File
- **Key Logs**:
  - Application startup/shutdown
  - Database migration with user/event counts
  - Fatal errors with proper cleanup

### 3. **Web Configuration** (EventScheduler.Web/Program.cs)
- **Log Location**: `logs/eventscheduler-web-.log`
- **Rolling Interval**: Daily
- **Retention**: 14 days
- **File Size Limit**: 10MB per file
- **Output**: Console (timestamped) + File
- **Key Logs**:
  - Application lifecycle events
  - API connection URL
  - Fatal errors with graceful shutdown

### 4. **CalendarView Component Logging**
Comprehensive logging added to trace the calendar initialization flow:

#### **OnInitializedAsync**
- Authentication check status
- Token retrieval and setting
- Event loading trigger

#### **OnAfterRenderAsync**
- Render state (firstRender, hasCheckedAuth, calendarInitialized, isLoading)
- Authentication recheck with JS interop
- Calendar initialization steps
- JSInterop call parameters
- Success/failure status

#### **LoadEvents**
- API call start
- Event count loaded
- Calendar update trigger
- Error details if failed

#### **SaveEvent**
- Edit vs Create mode
- Event details (ID, Title)
- Success/failure status

#### **OnEventDrop**
- Event ID and new times
- Event lookup status
- API update status
- Error recovery

#### **OnEventResize**
- Event ID and new times
- Duration update status
- Error recovery

## Log Files Location

After running the application, logs will be created in:
- **API**: `EventScheduler.Api/logs/eventscheduler-api-YYYYMMDD.log`
- **Web**: `EventScheduler.Web/logs/eventscheduler-web-YYYYMMDD.log`

## How to Use for Troubleshooting

### 1. **Start Both Applications**
```bash
# From project root
.\run-all.bat
```

### 2. **Navigate to Calendar View**
- Login to the application
- Browser will redirect to `/calendar-view`

### 3. **Check Console Output**
Look for timestamped log entries like:
```
[15:30:45 INF] CalendarView: OnInitializedAsync started
[15:30:45 INF] CalendarView: User authenticated in OnInitializedAsync
[15:30:45 INF] CalendarView: API token set from auth state
[15:30:45 INF] CalendarView: Loading events from API...
[15:30:45 INF] CalendarView: Loaded 5 events successfully
```

### 4. **Check Log Files**
Open the daily log files to see full execution trace:
- `EventScheduler.Web/logs/eventscheduler-web-20250115.log`
- `EventScheduler.Api/logs/eventscheduler-api-20250115.log`

### 5. **Diagnose Calendar Issue**
Look for these key indicators:

**✅ Authentication Working:**
```
CalendarView: User authenticated in OnInitializedAsync
CalendarView: API token set from auth state
```

**✅ Events Loading:**
```
CalendarView: Loaded 5 events successfully
```

**✅ Calendar Initialization:**
```
CalendarView: Starting calendar initialization - Event count: 5
CalendarView: Calling fullCalendarInterop.initialize with 5 events
CalendarView: ✅ Calendar initialized successfully!
```

**❌ Common Errors to Look For:**
```
CalendarView: User not authenticated, redirecting to login
CalendarView: Failed to load events from API
CalendarView: ❌ Calendar initialization returned false
CalendarView: ❌ Exception during calendar initialization
```

## Structured Logging Format

The logs use structured logging with property placeholders:
```csharp
Logger.LogInformation("CalendarView: Loaded {EventCount} events successfully", events.Count);
```

This allows for:
- Easy searching/filtering
- Log aggregation tools integration
- Better readability

## Log Levels Used

- **Information**: Normal flow (startup, success operations)
- **Warning**: Unexpected conditions (event not found, user not authenticated)
- **Error**: Failures with full exception details

## Recent Fixes

### Type Conversion Issues
**Problem**: `EventResponse.Status` is a `string`, but `GetEventColor()` expected `EventStatus` enum.

**Solution**: Added enum parsing in `ConvertToFullCalendarFormat()`:
```csharp
var status = Enum.TryParse<EventStatus>(e.Status, out var parsedStatus) 
    ? parsedStatus 
    : EventStatus.Scheduled;
```

**Added Using Directive**:
```csharp
@using EventScheduler.Domain.Entities
```

## Next Steps

1. **Stop current running instances** (if any)
2. **Start fresh with**: `.\run-all.bat`
3. **Login and navigate to calendar**
4. **Check logs to diagnose** why calendar isn't displaying
5. **Look for**:
   - Is authentication succeeding?
   - Are events loading from API?
   - Is JSInterop being called?
   - What error occurs during initialization?

## Benefits

✅ **Detailed execution trace** - See every step of calendar initialization  
✅ **Error context** - Full exception details with stack traces  
✅ **Performance insights** - Timing between lifecycle events  
✅ **Production-ready** - Daily rolling logs with retention policy  
✅ **Multi-target output** - Console for dev + Files for history  
✅ **Structured format** - Easy to parse and search  

## Troubleshooting Tips

### If Calendar Doesn't Display
1. **Check browser console** (F12) for JavaScript errors
2. **Check application logs** for C# errors
3. **Verify FullCalendar CDN** is accessible
4. **Check authentication** - logs will show if user is authenticated
5. **Check event loading** - logs will show event count
6. **Check JSInterop** - logs will show if `fullCalendarInterop.initialize` was called

### Common Log Patterns

**Successful Flow**:
```
[15:30:45 INF] CalendarView: OnInitializedAsync started
[15:30:45 INF] CalendarView: User authenticated in OnInitializedAsync
[15:30:45 INF] CalendarView: API token set from auth state
[15:30:45 INF] CalendarView: Loading events from API...
[15:30:45 INF] CalendarView: Loaded 5 events successfully
[15:30:45 INF] CalendarView: LoadEvents completed, isLoading set to false
[15:30:45 INF] CalendarView: OnAfterRenderAsync - FirstRender=False, HasCheckedAuth=True, CalendarInitialized=False, IsLoading=False
[15:30:45 INF] CalendarView: Starting calendar initialization - Event count: 5
[15:30:46 INF] CalendarView: Calling fullCalendarInterop.initialize with 5 events
[15:30:46 INF] CalendarView: ✅ Calendar initialized successfully!
```

**Authentication Failure**:
```
[15:30:45 INF] CalendarView: OnInitializedAsync started
[15:30:45 INF] CalendarView: User not authenticated yet, will check in OnAfterRenderAsync
[15:30:45 INF] CalendarView: OnAfterRenderAsync - FirstRender=True, HasCheckedAuth=False...
[15:30:45 INF] CalendarView: Re-checking authentication with JS interop available
[15:30:45 WRN] CalendarView: User not authenticated, redirecting to login
```

**Event Loading Failure**:
```
[15:30:45 INF] CalendarView: Loading events from API...
[15:30:45 ERR] CalendarView: Failed to load events from API
System.Net.Http.HttpRequestException: Connection refused
```

**Calendar Initialization Failure**:
```
[15:30:45 INF] CalendarView: Calling fullCalendarInterop.initialize with 5 events
[15:30:45 ERR] CalendarView: ❌ Calendar initialization returned false
```

---

**Status**: ✅ All code changes complete, packages restored, build succeeded, no errors  
**Ready to test**: Stop current instances and run `.\run-all.bat` to see logs in action
