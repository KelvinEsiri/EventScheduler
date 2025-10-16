# Date Validation Error Fix

## Issue
When users tried to create or update an event with an end date earlier than the start date, they received a generic "500 Internal Server Error" instead of a clear validation error message like "End date cannot be before start date".

### Error Log
```
System.Net.Http.HttpRequestException: Response status code does not indicate success: 500 (Internal Server Error).
   at System.Net.Http.HttpResponseMessage.EnsureSuccessStatusCode()
   at EventScheduler.Web.Services.ApiService.CreateEventAsync(CreateEventRequest request)
```

## Root Cause
The validation logic existed in `EventService.CreateEventAsync()` and `EventService.UpdateEventAsync()` which threw `InvalidOperationException` with the message "End date cannot be before start date". However:

1. **API Controller**: The `EventsController` caught this as a generic `Exception` and returned a 500 error instead of a 400 Bad Request
2. **API Service**: The `ApiService` used `EnsureSuccessStatusCode()` which threw a generic `HttpRequestException` without exposing the error message from the API response
3. **UI Components**: The Blazor components caught the exception but didn't display the specific validation error message to the user

## Solution

### 1. Updated EventsController (API)
Added specific handling for `InvalidOperationException` to return 400 Bad Request with the validation error message:

```csharp
[HttpPost]
public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
{
    try
    {
        // ... existing code ...
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning(ex, "Validation error creating event");
        return BadRequest(new { error = ex.Message });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating event");
        return StatusCode(500, new { error = "An error occurred while creating the event" });
    }
}
```

Similar changes were made to `UpdateEvent()` method.

### 2. Updated ApiService (Web)
Enhanced error handling to extract and expose error messages from API responses:

```csharp
public async Task<EventResponse?> CreateEventAsync(CreateEventRequest request)
{
    try
    {
        var response = await _httpClient.PostAsJsonAsync("/api/events", request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error creating event. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            
            // Try to extract the error message from the response
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (errorResponse?.Error != null)
                {
                    throw new InvalidOperationException(errorResponse.Error);
                }
            }
            catch (JsonException)
            {
                // If JSON deserialization fails, use the raw content
            }
            
            throw new HttpRequestException($"Error creating event: {response.StatusCode}");
        }
        
        return await response.Content.ReadFromJsonAsync<EventResponse>();
    }
    catch (InvalidOperationException)
    {
        // Re-throw validation errors
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating event");
        throw;
    }
}
```

Added `ErrorResponse` helper class:
```csharp
public class ErrorResponse
{
    public string? Error { get; set; }
}
```

### 3. Updated UI Components
Enhanced exception handling to display validation errors:

**CalendarView.razor:**
```csharp
catch (InvalidOperationException ex)
{
    // Handle validation errors with specific message
    Logger.LogWarning(ex, "CalendarView: Validation error saving event");
    ShowError(ex.Message);
}
catch (Exception ex)
{
    Logger.LogError(ex, "CalendarView: Error saving event");
    ShowError(isEditMode ? "Failed to update event." : "Failed to create event.");
}
```

**CalendarList.razor:**
```csharp
catch (InvalidOperationException ex)
{
    // Handle validation errors with specific message
    await JSRuntime.InvokeVoidAsync("alert", ex.Message);
}
catch (Exception)
{
    // Handle other errors
    var message = isEditMode ? "Failed to update event." : "Failed to create event.";
    await JSRuntime.InvokeVoidAsync("alert", message);
}
```

## Files Modified
1. `EventScheduler.Api/Controllers/EventsController.cs`
   - Added `InvalidOperationException` catch block in `CreateEvent()`
   - Added `InvalidOperationException` catch block in `UpdateEvent()`

2. `EventScheduler.Web/Services/ApiService.cs`
   - Added `ErrorResponse` class
   - Enhanced error handling in `CreateEventAsync()`
   - Enhanced error handling in `UpdateEventAsync()`

3. `EventScheduler.Web/Components/Pages/CalendarView.razor`
   - Added specific handling for `InvalidOperationException` in `SaveEvent()`

4. `EventScheduler.Web/Components/Pages/CalendarList.razor`
   - Added specific handling for `InvalidOperationException` in `SaveEvent()`

## How It Works Now

### Flow:
1. User creates/updates an event with end date before start date
2. **EventService** validates and throws `InvalidOperationException("End date cannot be before start date")`
3. **EventsController** catches the exception and returns:
   ```json
   Status: 400 Bad Request
   Body: { "error": "End date cannot be before start date" }
   ```
4. **ApiService** reads the error response and throws `InvalidOperationException` with the message
5. **UI Component** catches the `InvalidOperationException` and displays the message to the user via:
   - Toast notification (CalendarView)
   - Alert dialog (CalendarList)

### User Experience:
- ✅ Clear error message: "End date cannot be before start date"
- ✅ Proper HTTP status code (400 instead of 500)
- ✅ Error is logged at appropriate level (Warning for validation, Error for system issues)
- ✅ User can correct the input and try again

## Testing
1. Navigate to Calendar View or Calendar List
2. Try to create an event with:
   - Start Date: October 20, 2025
   - End Date: October 19, 2025
3. Click Save
4. Should see error message: "End date cannot be before start date"
5. Correct the dates and save successfully

## Date Fixed
October 16, 2025
