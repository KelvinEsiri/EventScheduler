# EventScheduler Logging Guide

## Quick Start

### View Logs in Real-Time
```bash
# Start applications (they will log to console)
.\run-all.bat

# Or tail the log files
Get-Content .\EventScheduler.Api\logs\eventscheduler-api-20251015.log -Wait
Get-Content .\EventScheduler.Web\logs\eventscheduler-web-20251015.log -Wait
```

## Log Configuration

### API Logging (EventScheduler.Api/Program.cs)
- **Output**: Console + File
- **File Path**: `logs/eventscheduler-api-<date>.log`
- **Min Level**: Information
- **Retention**: 14 days
- **Max File Size**: 10MB
- **Roll On Size**: Yes
- **Shared**: Yes (multiple processes can write)
- **Flush Interval**: 2 seconds

### Web Logging (EventScheduler.Web/Program.cs)
- **Output**: Console + File  
- **File Path**: `logs/eventscheduler-web-<date>.log`
- **Min Level**: Information (Warning for Microsoft logs)
- **Retention**: 14 days
- **Max File Size**: 10MB
- **Roll On Size**: Yes
- **Shared**: Yes
- **Flush Interval**: 2 seconds

## Log Formats

### Console Output
```
[15:30:45 INF] Message text
[HH:mm:ss LEVEL] Message
```

### File Output
```
2025-10-15 15:30:45.123 +01:00 [INF] Message text
yyyy-MM-dd HH:mm:ss.fff zzz [LEVEL] Message
```

## Log Levels

| Level | Usage | Example |
|-------|-------|---------|
| **INF** (Information) | Normal operations | User login, events loaded, calendar initialized |
| **WRN** (Warning) | Unexpected but handled | Event not found, user not authenticated |
| **ERR** (Error) | Failures with recovery | API call failed, calendar initialization failed |
| **FTL** (Fatal) | Application crash | Unhandled exception at startup |

## CalendarView Logging

### Lifecycle Events

#### OnInitializedAsync
```csharp
Logger.LogInformation("CalendarView: OnInitializedAsync started");
Logger.LogInformation("CalendarView: User authenticated in OnInitializedAsync");
Logger.LogInformation("CalendarView: API token set from auth state");
```

#### OnAfterRenderAsync
```csharp
Logger.LogInformation("CalendarView: OnAfterRenderAsync - FirstRender={FirstRender}, HasCheckedAuth={HasCheckedAuth}, CalendarInitialized={CalendarInitialized}, IsLoading={IsLoading}");
Logger.LogInformation("CalendarView: Re-checking authentication with JS interop available");
Logger.LogWarning("CalendarView: User not authenticated, redirecting to login");
Logger.LogInformation("CalendarView: Starting calendar initialization - Event count: {EventCount}");
Logger.LogInformation("CalendarView: Calling fullCalendarInterop.initialize with {EventCount} events");
Logger.LogInformation("CalendarView: ✅ Calendar initialized successfully!");
Logger.LogError("CalendarView: ❌ Calendar initialization returned false");
```

#### LoadEvents
```csharp
Logger.LogInformation("CalendarView: Loading events from API...");
Logger.LogInformation("CalendarView: Loaded {EventCount} events successfully");
Logger.LogInformation("CalendarView: Calendar already initialized, updating events");
Logger.LogError(ex, "CalendarView: Failed to load events from API");
Logger.LogInformation("CalendarView: LoadEvents completed, isLoading set to false");
```

### Event Operations

#### SaveEvent
```csharp
Logger.LogInformation("CalendarView: Saving event - IsEditMode={IsEditMode}, EventId={EventId}, Title={Title}");
Logger.LogInformation("CalendarView: Event {EventId} updated successfully");
Logger.LogInformation("CalendarView: New event created successfully - Title={Title}");
Logger.LogError(ex, "CalendarView: Failed to save event - IsEditMode={IsEditMode}");
```

#### OnEventDrop
```csharp
Logger.LogInformation("CalendarView: Event {EventId} dropped - NewStart={NewStart}, NewEnd={NewEnd}, AllDay={AllDay}");
Logger.LogWarning("CalendarView: Event {EventId} not found for drop operation");
Logger.LogInformation("CalendarView: Event {EventId} rescheduled successfully");
Logger.LogError(ex, "CalendarView: Failed to reschedule event {EventId}");
```

#### OnEventResize
```csharp
Logger.LogInformation("CalendarView: Event {EventId} resized - NewStart={NewStart}, NewEnd={NewEnd}");
Logger.LogWarning("CalendarView: Event {EventId} not found for resize operation");
Logger.LogInformation("CalendarView: Event {EventId} duration updated successfully");
Logger.LogError(ex, "CalendarView: Failed to update event {EventId} duration");
```

## API Logging

### Startup
```csharp
Log.Information("========================================");
Log.Information("Starting EventScheduler API");
Log.Information("========================================");
Log.Information("Database migration completed successfully");
Log.Information("Database contains {UserCount} users and {EventCount} events");
Log.Information("EventScheduler API listening on http://localhost:5006");
Log.Information("API is ready to handle requests");
```

### Shutdown
```csharp
Log.Information("Shutting down EventScheduler API");
Log.Fatal(ex, "API terminated unexpectedly");
```

## Web Logging

### Startup
```csharp
Log.Information("========================================");
Log.Information("Starting EventScheduler Web Application");
Log.Information("========================================");
Log.Information("EventScheduler Web listening on http://localhost:5292");
Log.Information("Connected to API at: {ApiUrl}");
Log.Information("Web application is ready");
```

### Shutdown
```csharp
Log.Information("Shutting down EventScheduler Web");
Log.Fatal(ex, "Web application terminated unexpectedly");
```

## Structured Logging

### Property Placeholders
Use `{PropertyName}` for structured logging:
```csharp
Logger.LogInformation("User {UserId} loaded {EventCount} events", userId, count);
```

**Benefits**:
- Searchable properties in log aggregators
- Type-safe logging
- Better performance than string interpolation
- Consistent formatting

### Don't Use String Interpolation
❌ **Bad**:
```csharp
Logger.LogInformation($"User {userId} loaded {count} events");
```

✅ **Good**:
```csharp
Logger.LogInformation("User {UserId} loaded {EventCount} events", userId, count);
```

## Filtering Logs

### By Level
```bash
# PowerShell - Show only errors
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "\[ERR\]"

# PowerShell - Show only warnings and errors
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "\[WRN\]|\[ERR\]"
```

### By Component
```bash
# Show only CalendarView logs
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "CalendarView"

# Show authentication logs
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "authenticated"
```

### By Time Range
```bash
# Show logs from specific time
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "2025-10-15 15:[3-4]"
```

## Log File Management

### Automatic Cleanup
- Files older than 14 days are automatically deleted
- Files exceeding 10MB are rolled to a new file

### Manual Cleanup
```bash
# Delete all log files
Remove-Item .\EventScheduler.Api\logs\*.log
Remove-Item .\EventScheduler.Web\logs\*.log

# Delete old logs (keep today's)
Get-ChildItem .\EventScheduler.Api\logs\*.log | Where-Object {$_.LastWriteTime -lt (Get-Date).Date} | Remove-Item
Get-ChildItem .\EventScheduler.Web\logs\*.log | Where-Object {$_.LastWriteTime -lt (Get-Date).Date} | Remove-Item
```

## Debugging Workflow

### 1. Reproduce the Issue
```bash
# Start with clean logs
Remove-Item .\EventScheduler.Web\logs\*.log

# Start applications
.\run-all.bat

# Perform the action that causes the issue
```

### 2. Check Console
Look for immediate error messages in the console output.

### 3. Check Log Files
```bash
# View the full log
Get-Content .\EventScheduler.Web\logs\eventscheduler-web-*.log

# Search for errors
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "\[ERR\]" -Context 5
```

### 4. Analyze the Flow
Look for the sequence of events leading to the error:
1. Was authentication successful?
2. Did events load?
3. Did calendar initialization start?
4. Where did it fail?

### 5. Check Related Logs
If the Web log shows an API call failing, check the API log for details.

## Common Issues

### Calendar Not Displaying

**Check for**:
```bash
# Authentication
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "authenticated|redirecting"

# Event loading
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "Loading events|Loaded.*events"

# Calendar initialization
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "calendar initialization|initialized successfully"
```

### Events Not Saving

**Check for**:
```bash
# Save attempts
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "Saving event"

# API errors
Get-Content .\EventScheduler.Api\logs\*.log | Select-String "POST.*events"
```

### Authentication Failures

**Check for**:
```bash
# Login attempts
Get-Content .\EventScheduler.Api\logs\*.log | Select-String "login|authentication"

# Token issues
Get-Content .\EventScheduler.Web\logs\*.log | Select-String "token"
```

## Production Considerations

### Log Aggregation
Consider using tools like:
- **Seq**: Local log aggregation server
- **Elasticsearch + Kibana**: Scalable log analysis
- **Application Insights**: Azure cloud logging

### Configuration Changes for Production
```csharp
.MinimumLevel.Override("Microsoft", LogEventLevel.Error)
.MinimumLevel.Override("System", LogEventLevel.Error)
```

### Security
- **Don't log sensitive data**: Passwords, tokens, personal information
- **Sanitize user input** before logging
- **Use structured logging** to avoid injection attacks

### Performance
- **Async logging**: Serilog uses async by default
- **Batching**: Configure batch size for high-volume scenarios
- **Sampling**: Log only a percentage of requests in high-traffic areas

---

**Last Updated**: October 15, 2025  
**Version**: 1.0  
**Serilog Version**: 9.0.0
