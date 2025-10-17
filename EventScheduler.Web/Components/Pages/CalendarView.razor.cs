using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using EventScheduler.Web.Services;
using System.Text.Json;

namespace EventScheduler.Web.Components.Pages;

public partial class CalendarView : IAsyncDisposable
{
    [Inject] private ApiService ApiService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<CalendarView> Logger { get; set; } = default!;
    [Inject] private EventUIHelperService UIHelper { get; set; } = default!;

    private List<EventResponse> events = new();
    private bool isLoading = true;
    private bool calendarInitialized = false;
    private bool initializationAttempted = false;
    private DotNetObjectReference<CalendarView>? dotNetHelper;
    private bool hasCheckedAuth = false;
    private string? errorMessage;
    private string? successMessage;
    private int currentUserId = 0;
    
    private HubConnection? hubConnection;
    private bool isConnected = false;
    private string? connectionStatus;
    private readonly HashSet<int> pendingLocalChanges = new();
    private DateTime? lastLocalOperationTime = null;
    
    private bool showModal = false;
    private bool isEditMode = false;
    private int editEventId = 0;
    private bool isSaving = false;
    
    private bool showDetailsModal = false;
    private EventResponse? selectedEvent = null;
    private bool isUserOrganizer = false;
    private bool isUserParticipant = false;
    
    private bool showDayEventsModal = false;
    private DateTime? selectedDate = null;
    private List<EventResponse> dayEvents = new();
    
    // Form model
    private CreateEventRequest eventRequest = new CreateEventRequest
    {
        Title = "",
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddHours(1)
    };

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("CalendarView: Initializing component (prerender)");
        
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            Logger.LogInformation("CalendarView: User authenticated in memory");
            hasCheckedAuth = true;
            
            if (int.TryParse(user.FindFirst("userId")?.Value, out int userId))
            {
                currentUserId = userId;
            }
            
            var token = user.FindFirst("token")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                ApiService.SetToken(token);
                Logger.LogInformation("CalendarView: Authentication configured");
                
                try
                {
                    await Task.WhenAll(
                        InitializeSignalR(),
                        LoadEvents()
                    );
                }
                catch (TaskCanceledException ex)
                {
                    Logger.LogWarning(ex, "CalendarView: Initialization was canceled (likely due to navigation)");
                }
            }
        }
        else
        {
            Logger.LogInformation("CalendarView: Auth not yet loaded, will check in OnAfterRenderAsync");
        }
    }

    private async Task InitializeSignalR()
    {
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("token")?.Value;
            
            if (string.IsNullOrEmpty(token))
            {
                connectionStatus = "Real-time updates unavailable";
                Logger.LogWarning("SignalR: No authentication token available");
                return;
            }
            
            var hubUrl = "http://localhost:5006/hubs/events";
            
            hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .WithAutomaticReconnect()
                .ConfigureLogging(logging => {
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            hubConnection.Reconnecting += OnReconnecting;
            hubConnection.Reconnected += OnReconnected;
            hubConnection.Closed += OnClosed;

            RegisterSignalRHandlers();

            await hubConnection.StartAsync();
            
            isConnected = true;
            connectionStatus = "Connected to real-time updates";
            Logger.LogInformation("SignalR: Connected (Connection ID: {ConnectionId})", hubConnection.ConnectionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SignalR: Connection failed");
            connectionStatus = "Real-time updates unavailable";
            isConnected = false;
        }
    }

    private void RegisterSignalRHandlers()
    {
        if (hubConnection == null) return;

        hubConnection.On<EventResponse>("EventCreated", async (eventData) => {
            await InvokeAsync(async () => {
                if (IsRecentLocalOperation())
                {
                    lastLocalOperationTime = null;
                }
                else
                {
                    ShowSuccess($"Event '{eventData.Title}' created!");
                }
                
                events.Add(eventData);
                await JSRuntime.InvokeVoidAsync("addEventToCalendar", eventData);
                StateHasChanged();
            });
        });

        hubConnection.On<EventResponse>("EventUpdated", async (eventData) => {
            await InvokeAsync(async () => {
                if (!pendingLocalChanges.Remove(eventData.Id))
                {
                    ShowSuccess($"Event '{eventData.Title}' updated!");
                }
                
                UpdateEventInList(eventData);
                await JSRuntime.InvokeVoidAsync("updateEventInCalendar", eventData);
                StateHasChanged();
            });
        });

        hubConnection.On<object>("EventDeleted", async (deletedEventInfo) => {
            await InvokeAsync(async () => {
                var eventId = ExtractEventId(deletedEventInfo);
                
                if (!pendingLocalChanges.Remove(eventId))
                {
                    ShowSuccess("Event deleted!");
                }
                
                RemoveEventFromList(eventId);
                await JSRuntime.InvokeVoidAsync("removeEventFromCalendar", eventId);
                StateHasChanged();
            });
        });
    }

    private bool IsRecentLocalOperation() => 
        lastLocalOperationTime.HasValue && 
        (DateTime.UtcNow - lastLocalOperationTime.Value).TotalSeconds < 2;

    private void UpdateEventInList(EventResponse eventData)
    {
        var existingEvent = events.FirstOrDefault(e => e.Id == eventData.Id);
        if (existingEvent != null) events.Remove(existingEvent);
        events.Add(eventData);
    }

    private void RemoveEventFromList(int eventId)
    {
        var eventToRemove = events.FirstOrDefault(e => e.Id == eventId);
        if (eventToRemove != null) events.Remove(eventToRemove);
    }

    private int ExtractEventId(object deletedEventInfo)
    {
        var json = JsonSerializer.Serialize(deletedEventInfo);
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    private void ShowSuccess(string message)
    {
        successMessage = message;
        _ = Task.Delay(3000).ContinueWith(_ => {
            successMessage = null;
            InvokeAsync(StateHasChanged);
        });
    }

    private void ShowError(string message)
    {
        errorMessage = message;
        _ = Task.Delay(5000).ContinueWith(_ => {
            errorMessage = null;
            InvokeAsync(StateHasChanged);
        });
    }

    private Task OnReconnecting(Exception? exception)
    {
        Logger.LogWarning(exception, "SignalR: ⚠️ Connection lost, attempting to reconnect...");
        connectionStatus = "Reconnecting to real-time updates...";
        isConnected = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        Logger.LogInformation("SignalR: ✅ Reconnected successfully! New Connection ID: {ConnectionId}", connectionId);
        connectionStatus = "Reconnected to real-time updates";
        isConnected = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnClosed(Exception? exception)
    {
        Logger.LogError(exception, "SignalR: ❌ Connection closed");
        connectionStatus = "Disconnected from real-time updates";
        isConnected = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Logger.LogInformation("CalendarView: OnAfterRenderAsync called - firstRender={FirstRender}, calendarInitialized={CalendarInitialized}, isLoading={IsLoading}", 
            firstRender, calendarInitialized, isLoading);
        
        if (firstRender && !hasCheckedAuth)
        {
            Logger.LogInformation("CalendarView: OnAfterRenderAsync - checking auth with localStorage available");
            hasCheckedAuth = true;
            
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            
            Logger.LogInformation("CalendarView: Auth check result - IsAuthenticated: {IsAuth}", user.Identity?.IsAuthenticated);
            
            if (user.Identity?.IsAuthenticated != true)
            {
                Logger.LogWarning("CalendarView: Not authenticated after localStorage check, redirecting to login");
                NavigationManager.NavigateTo("/login", forceLoad: true);
                return;
            }
            
            if (int.TryParse(user.FindFirst("userId")?.Value, out int userId))
            {
                currentUserId = userId;
            }
            
            var token = user.FindFirst("token")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                ApiService.SetToken(token);
                Logger.LogInformation("CalendarView: Token set, loading events and initializing SignalR");
                
                await Task.WhenAll(
                    InitializeSignalR(),
                    LoadEvents()
                );
                
                StateHasChanged();
            }
            else
            {
                Logger.LogWarning("CalendarView: No token found, redirecting to login");
                NavigationManager.NavigateTo("/login", forceLoad: true);
                return;
            }
        }
        
        if (firstRender && !calendarInitialized && !isLoading)
        {
            Logger.LogInformation("CalendarView: Conditions met for calendar initialization");
            await InitializeCalendar();
        }
        else if (!firstRender && hasCheckedAuth && !calendarInitialized && !isLoading)
        {
            Logger.LogInformation("CalendarView: Initializing calendar after data load");
            await InitializeCalendar();
        }
    }

    private async Task InitializeCalendar()
    {
        if (calendarInitialized || initializationAttempted)
        {
            Logger.LogInformation("CalendarView: Calendar already initialized or attempt in progress, skipping");
            return;
        }

        initializationAttempted = true;
        
        try
        {
            Logger.LogInformation("CalendarView: Attempting to initialize calendar with {Count} events", events.Count);
            
            var elementExists = await JSRuntime.InvokeAsync<bool>("eval", "document.getElementById('calendar') !== null");
            if (!elementExists)
            {
                Logger.LogError("CalendarView: Calendar element 'calendar' not found in DOM");
                initializationAttempted = false;
                return;
            }
            
            try
            {
                var hasExistingCalendar = await JSRuntime.InvokeAsync<bool>("eval", 
                    "window.fullCalendarInterop && window.fullCalendarInterop.calendars && window.fullCalendarInterop.calendars['calendar'] !== undefined");
                
                if (hasExistingCalendar)
                {
                    Logger.LogWarning("CalendarView: Found existing calendar instance, destroying it first");
                    await JSRuntime.InvokeVoidAsync("fullCalendarInterop.destroy", "calendar");
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "CalendarView: Error checking/destroying existing calendar");
            }
            
            dotNetHelper = DotNetObjectReference.Create(this);
            var calendarEvents = ConvertToFullCalendarFormat();
            
            await Task.Delay(500);
            
            var initialized = await JSRuntime.InvokeAsync<bool>("fullCalendarInterop.initialize", 
                "calendar", dotNetHelper, calendarEvents, true);
            
            if (initialized)
            {
                calendarInitialized = true;
                Logger.LogInformation("CalendarView: Calendar initialized successfully with {Count} events", calendarEvents.Length);
                
                await Task.Delay(100);
                await JSRuntime.InvokeVoidAsync("fullCalendarInterop.updateSize", "calendar");
                Logger.LogInformation("CalendarView: Calendar size updated");
            }
            else
            {
                Logger.LogWarning("CalendarView: Calendar initialization returned false");
                initializationAttempted = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarView: Error initializing calendar");
            initializationAttempted = false;
        }
    }

    private async Task LoadEvents()
    {
        try
        {
            isLoading = true;
            errorMessage = null;
            
            events = await ApiService.GetAllEventsAsync();
            
            Logger.LogInformation("CalendarView: Loaded {Count} events", events.Count);
            
            if (calendarInitialized)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("fullCalendarInterop.updateEvents", ConvertToFullCalendarFormat());
                }
                catch (JSDisconnectedException)
                {
                    Logger.LogWarning("CalendarView: Circuit disconnected while updating calendar");
                }
            }
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogWarning(ex, "CalendarView: Load events was canceled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarView: Failed to load events");
            ShowError("Failed to load events. Please try again.");
        }
        finally
        {
            isLoading = false;
            Logger.LogInformation("CalendarView: Loading complete, isLoading={IsLoading}", isLoading);
            
            if (!calendarInitialized)
            {
                StateHasChanged();
                await Task.Delay(100);
                
                if (!calendarInitialized)
                {
                    await InitializeCalendar();
                }
            }
        }
    }

    private object[] ConvertToFullCalendarFormat()
    {
        return events.Select(e => new
        {
            id = e.Id.ToString(),
            title = e.Title,
            start = e.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
            end = e.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
            allDay = e.IsAllDay,
            backgroundColor = GetEventColor(e.Status, e.EventType),
            borderColor = GetEventColor(e.Status, e.EventType),
            extendedProps = new
            {
                description = e.Description,
                location = e.Location,
                status = e.Status,
                eventType = e.EventType,
                isPublic = e.IsPublic
            }
        }).ToArray();
    }

    private string GetEventColor(string status, string eventType)
    {
        return status.ToLower() switch
        {
            "completed" => "#10b981",
            "cancelled" => "#ef4444",
            "in progress" => "#f59e0b",
            _ => eventType switch
            {
                "Festival" => "#ec4899",
                "Interview" => "#8b5cf6",
                "Birthday" => "#f97316",
                "Exam" => "#dc2626",
                "Appointment" => "#06b6d4",
                "Meeting" => "#3b82f6",
                "Reminder" => "#eab308",
                "Task" => "#14b8a6",
                _ => "#6366f1"
            }
        };
    }

    private void ShowCreateModal()
    {
        isEditMode = false;
        editEventId = 0;
        eventRequest = new CreateEventRequest
        {
            Title = "",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1),
            EventType = "Other",
            IsPublic = false
        };
        showModal = true;
    }

    private void CloseModal()
    {
        showModal = false;
        isEditMode = false;
        editEventId = 0;
        StateHasChanged();
    }

    private void CloseDetailsModal()
    {
        showDetailsModal = false;
        selectedEvent = null;
        isUserOrganizer = false;
        isUserParticipant = false;
        StateHasChanged();
    }

    [JSInvokable]
    public Task OnDateClick(string dateStr)
    {
        try
        {
            Logger.LogInformation("CalendarView: Date clicked - {DateStr}", dateStr);
            var date = DateTime.Parse(dateStr);
            ShowDayEventsModal(date);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarView: Error handling date click");
            errorMessage = "Failed to show events for selected date.";
        }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnDateSelect(string startStr, string endStr, bool allDay)
    {
        try
        {
            Logger.LogInformation("CalendarView: Date range selected - {Start} to {End}, AllDay: {AllDay}", startStr, endStr, allDay);
            var startDate = DateTime.Parse(startStr);
            var endDate = DateTime.Parse(endStr);
            
            ShowCreateModalForDateRange(startDate, endDate, allDay);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarView: Error handling date selection");
            errorMessage = "Failed to create event for selected date range.";
        }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnEventClick(int eventId)
    {
        try
        {
            Logger.LogInformation("CalendarView: Event clicked - {EventId}", eventId);
            
            var eventItem = events.FirstOrDefault(e => e.Id == eventId);
            if (eventItem != null)
            {
                await ShowEventDetails(eventItem);
                StateHasChanged(); // Force UI update
            }
            else
            {
                Logger.LogWarning("CalendarView: Event with ID {EventId} not found in events list", eventId);
                errorMessage = "Event not found.";
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarView: Error handling event click");
            errorMessage = "Failed to show event details.";
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task OnEventDrop(int eventId, string newStartStr, string newEndStr, bool allDay)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(newStartStr) || string.IsNullOrEmpty(newEndStr))
            {
                Logger.LogError("CalendarView: Invalid date strings - Start: '{Start}', End: '{End}'", newStartStr, newEndStr);
                ShowError("❌ Invalid date format. Please try again.");
                await JSRuntime.InvokeVoidAsync("fullCalendarInterop.revertEvent", eventId);
                return;
            }
            
            pendingLocalChanges.Add(eventId);
            
            var eventItem = events.FirstOrDefault(e => e.Id == eventId);
            if (eventItem == null)
            {
                ShowError("❌ Event not found.");
                pendingLocalChanges.Remove(eventId);
                await JSRuntime.InvokeVoidAsync("fullCalendarInterop.revertEvent", eventId);
                return;
            }

            // Parse dates with error handling
            DateTime newStart, newEnd;
            try
            {
                newStart = DateTime.Parse(newStartStr);
                newEnd = DateTime.Parse(newEndStr);
            }
            catch (FormatException ex)
            {
                Logger.LogError(ex, "CalendarView: Failed to parse dates - Start: '{Start}', End: '{End}'", newStartStr, newEndStr);
                ShowError("❌ Invalid date format. Please try again.");
                pendingLocalChanges.Remove(eventId);
                await JSRuntime.InvokeVoidAsync("fullCalendarInterop.revertEvent", eventId);
                return;
            }

            // Optimistic update - update UI immediately for instant feedback
            var oldStart = eventItem.StartDate;
            var oldEnd = eventItem.EndDate;
            var oldAllDay = eventItem.IsAllDay;
            
            eventItem.StartDate = newStart;
            eventItem.EndDate = newEnd;
            eventItem.IsAllDay = allDay;

            var updateRequest = CreateUpdateRequestFromEvent(
                eventItem, 
                newStart, 
                newEnd, 
                allDay
            );

            // Save to server
            await ApiService.UpdateEventAsync(eventId, updateRequest);
            
            // Show success with date info
            var dateStr = eventItem.StartDate.ToString("MMM dd, yyyy");
            if (!allDay)
            {
                dateStr += $" at {eventItem.StartDate:hh:mm tt}";
            }
            ShowSuccess($"✅ Event moved to {dateStr}");
            
            Logger.LogInformation("CalendarView: Event {EventId} rescheduled successfully", eventId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarView: Error rescheduling event");
            ShowError("❌ Failed to reschedule event. Please try again.");
            pendingLocalChanges.Remove(eventId);
            
            // Revert the event on the calendar
            await JSRuntime.InvokeVoidAsync("fullCalendarInterop.revertEvent", eventId);
        }
    }

    [JSInvokable]
    public async Task OnEventResize(int eventId, string newStartStr, string newEndStr)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(newStartStr) || string.IsNullOrEmpty(newEndStr))
            {
                Logger.LogError("CalendarView: Invalid date strings for resize - Start: '{Start}', End: '{End}'", newStartStr, newEndStr);
                ShowError("❌ Invalid date format. Please try again.");
                await JSRuntime.InvokeVoidAsync("fullCalendarInterop.revertEvent", eventId);
                return;
            }
            
            pendingLocalChanges.Add(eventId);
            
            var eventItem = events.FirstOrDefault(e => e.Id == eventId);
            if (eventItem == null)
            {
                ShowError("❌ Event not found.");
                pendingLocalChanges.Remove(eventId);
                await JSRuntime.InvokeVoidAsync("fullCalendarInterop.revertEvent", eventId);
                return;
            }

            // Parse dates with error handling
            DateTime newStart, newEnd;
            try
            {
                newStart = DateTime.Parse(newStartStr);
                newEnd = DateTime.Parse(newEndStr);
            }
            catch (FormatException ex)
            {
                Logger.LogError(ex, "CalendarView: Failed to parse resize dates - Start: '{Start}', End: '{End}'", newStartStr, newEndStr);
                ShowError("❌ Invalid date format. Please try again.");
                pendingLocalChanges.Remove(eventId);
                await JSRuntime.InvokeVoidAsync("fullCalendarInterop.revertEvent", eventId);
                return;
            }

            // Optimistic update - update UI immediately
            var oldStart = eventItem.StartDate;
            var oldEnd = eventItem.EndDate;
            
            eventItem.StartDate = newStart;
            eventItem.EndDate = newEnd;

            var updateRequest = CreateUpdateRequestFromEvent(
                eventItem, 
                newStart, 
                newEnd
            );

            // Save to server
            await ApiService.UpdateEventAsync(eventId, updateRequest);
            
            // Calculate and show duration
            var duration = eventItem.EndDate - eventItem.StartDate;
            var durationStr = duration.Hours > 0 
                ? $"{duration.Hours}h {duration.Minutes}m" 
                : $"{duration.Minutes}m";
            
            ShowSuccess($"✅ Duration updated to {durationStr}");
            
            Logger.LogInformation("CalendarView: Event {EventId} resized successfully", eventId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarView: Error resizing event");
            ShowError("❌ Failed to update event duration. Please try again.");
            pendingLocalChanges.Remove(eventId);
            
            // Revert the event on the calendar
            await JSRuntime.InvokeVoidAsync("fullCalendarInterop.revertEvent", eventId);
        }
    }

    // Helper method to create update request from existing event
    private UpdateEventRequest CreateUpdateRequestFromEvent(
        EventResponse eventItem, 
        DateTime newStart, 
        DateTime newEnd, 
        bool? allDay = null)
    {
        return new UpdateEventRequest
        {
            Title = eventItem.Title,
            Description = eventItem.Description,
            StartDate = newStart,
            EndDate = newEnd,
            Location = eventItem.Location,
            IsAllDay = allDay ?? eventItem.IsAllDay,
            Color = eventItem.Color,
            CategoryId = eventItem.CategoryId,
            Status = eventItem.Status,
            EventType = eventItem.EventType,
            IsPublic = eventItem.IsPublic,
            Invitations = eventItem.Invitations?.Select(i => new EventInvitationRequest
            {
                InviteeName = i.InviteeName,
                InviteeEmail = i.InviteeEmail
            }).ToList()
        };
    }

    // Event CRUD operations
    private async Task SaveEvent()
    {
        try
        {
            isSaving = true;

            if (isEditMode)
            {
                pendingLocalChanges.Add(editEventId);
                
                var updateRequest = CreateUpdateRequest();
                await ApiService.UpdateEventAsync(editEventId, updateRequest);
                ShowSuccess("Event updated successfully!");
            }
            else
            {
                lastLocalOperationTime = DateTime.UtcNow;
                await ApiService.CreateEventAsync(eventRequest);
                ShowSuccess("Event created successfully!");
            }

            CloseModal();
            Logger.LogInformation("CalendarView: Event saved, awaiting SignalR notification");
        }
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
        finally
        {
            isSaving = false;
        }
    }

    private UpdateEventRequest CreateUpdateRequest() => new()
    {
        Title = eventRequest.Title,
        Description = eventRequest.Description,
        StartDate = eventRequest.StartDate,
        EndDate = eventRequest.EndDate,
        Location = eventRequest.Location,
        IsAllDay = eventRequest.IsAllDay,
        Color = eventRequest.Color,
        CategoryId = eventRequest.CategoryId,
        EventType = eventRequest.EventType,
        IsPublic = eventRequest.IsPublic,
        Invitations = eventRequest.Invitations
    };

    private async Task DeleteEventFromDetails(int eventId)
    {
        try
        {
            var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this event?");
            
            if (!confirmed) return;

            pendingLocalChanges.Add(eventId);
            
            // Optimistic UI update - remove immediately
            RemoveEventFromList(eventId);
            await JSRuntime.InvokeVoidAsync("removeEventFromCalendar", eventId);
            
            // Call API
            await ApiService.DeleteEventAsync(eventId);
            ShowSuccess("Event deleted successfully!");
            CloseDetailsModal();
            
            Logger.LogInformation("CalendarView: Event {EventId} deleted", eventId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarView: Error deleting event");
            ShowError("Failed to delete event.");
            pendingLocalChanges.Remove(eventId);
            // Restore events on failure
            await LoadEvents();
        }
    }

    // Helper methods for modal display
    private void ShowDayEventsModal(DateTime date)
    {
        selectedDate = date;
        
        // Filter events for the selected day
        dayEvents = events.Where(e => 
            e.StartDate.Date <= date.Date && 
            e.EndDate.Date >= date.Date
        ).OrderBy(e => e.StartDate).ToList();
        
        Logger.LogInformation("CalendarView: Showing {Count} events for date {Date}", dayEvents.Count, date.ToString("yyyy-MM-dd"));
        showDayEventsModal = true;
        StateHasChanged(); // Force UI update
    }
    
    private void CloseDayEventsModal()
    {
        showDayEventsModal = false;
        selectedDate = null;
        dayEvents.Clear();
        StateHasChanged(); // Force UI update
    }
    
    private void TransitionToCreateEventModal()
    {
        // Save the date before closing the modal
        var dateToUse = selectedDate ?? DateTime.Now;
        
        // Close the day events modal
        showDayEventsModal = false;
        dayEvents.Clear();
        
        // Open the create event modal with the saved date
        isEditMode = false;
        editEventId = 0;
        eventRequest = new CreateEventRequest
        {
            Title = "",
            StartDate = dateToUse.Date.AddHours(9), // Default to 9 AM
            EndDate = dateToUse.Date.AddHours(10), // Default 1 hour duration
            EventType = "Other",
            IsPublic = false
        };
        showModal = true;
        
        // Clear selectedDate after use
        selectedDate = null;
        
        Logger.LogInformation("CalendarView: Transitioned from day events modal to create event modal for date {Date}", dateToUse.ToString("yyyy-MM-dd"));
        StateHasChanged(); // Force UI update
    }
    
    private void ShowCreateModalForDate(DateTime date)
    {
        isEditMode = false;
        editEventId = 0;
        eventRequest = new CreateEventRequest
        {
            Title = "",
            StartDate = date.Date.AddHours(9), // Default to 9 AM
            EndDate = date.Date.AddHours(10), // Default 1 hour duration
            EventType = "Other",
            IsPublic = false
        };
        showModal = true;
        StateHasChanged(); // Force UI update
    }

    private void ShowCreateModalForDateRange(DateTime startDate, DateTime endDate, bool allDay)
    {
        isEditMode = false;
        editEventId = 0;
        eventRequest = new CreateEventRequest
        {
            Title = "",
            StartDate = startDate,
            EndDate = endDate,
            IsAllDay = allDay,
            EventType = "Other",
            IsPublic = false
        };
        showModal = true;
        StateHasChanged(); // Force UI update
    }

    private async Task ShowEventDetailsFromDayList(EventResponse eventItem)
    {
        Logger.LogInformation("CalendarView: Transitioning from day events modal to event details for: {EventTitle} (ID: {EventId})", eventItem.Title, eventItem.Id);
        
        // Close the day events modal first
        showDayEventsModal = false;
        selectedDate = null;
        dayEvents.Clear();
        
        // Then show the event details
        await ShowEventDetails(eventItem);
    }
    
    private async Task ShowEventDetails(EventResponse eventItem)
    {
        selectedEvent = eventItem;
        
        // Use cached user ID instead of fetching auth state again
        isUserOrganizer = eventItem.UserId == currentUserId;
        isUserParticipant = false; // TODO: Check participants list
        
        showDetailsModal = true;
        Logger.LogInformation("CalendarView: Showing details for event {EventId}", eventItem.Id);
        StateHasChanged();
        
        await Task.CompletedTask;
    }

    private void EditEventFromDetails(EventResponse eventItem)
    {
        isEditMode = true;
        editEventId = eventItem.Id;
        eventRequest = new CreateEventRequest
        {
            Title = eventItem.Title,
            Description = eventItem.Description,
            StartDate = eventItem.StartDate,
            EndDate = eventItem.EndDate,
            Location = eventItem.Location,
            IsAllDay = eventItem.IsAllDay,
            Color = eventItem.Color,
            CategoryId = eventItem.CategoryId,
            EventType = eventItem.EventType,
            IsPublic = eventItem.IsPublic,
            Invitations = eventItem.Invitations?.Select(i => new EventInvitationRequest
            {
                InviteeName = i.InviteeName,
                InviteeEmail = i.InviteeEmail
            }).ToList()
        };
        
        CloseDetailsModal();
        showModal = true;
        StateHasChanged(); // Force UI update
    }

    private async Task JoinEvent()
    {
        // This would be implemented with a proper API endpoint
        // For now, just show a success message
        successMessage = "You've joined the event!";
        CloseDetailsModal();
        await LoadEvents();
        StateHasChanged(); // Force UI update
    }

    private void AddInvitation()
    {
        if (eventRequest.Invitations == null)
        {
            eventRequest.Invitations = new List<EventInvitationRequest>();
        }
        
        eventRequest.Invitations.Add(new EventInvitationRequest
        {
            InviteeName = "",
            InviteeEmail = ""
        });
    }

    private bool confirm(string message)
    {
        // This is a simplified version. In production, you'd use JSInterop for a proper confirm dialog
        // For now, we'll just return true
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        Logger.LogInformation("CalendarView: Disposing component");
        
        try
        {
            // Dispose SignalR connection first
            if (hubConnection != null)
            {
                Logger.LogInformation("CalendarView: Disposing SignalR connection");
                await hubConnection.DisposeAsync();
                hubConnection = null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "CalendarView: Error disposing SignalR connection");
        }

        try
        {
            // Dispose calendar and dotnet helper
            if (dotNetHelper != null)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("fullCalendarInterop.destroy");
                }
                catch (JSDisconnectedException)
                {
                    // Circuit already disconnected, ignore
                }
                catch (TaskCanceledException)
                {
                    // Operation canceled, ignore
                }
                
                dotNetHelper.Dispose();
                dotNetHelper = null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "CalendarView: Error disposing calendar");
        }
        
        Logger.LogInformation("CalendarView: Component disposed");
    }
}
