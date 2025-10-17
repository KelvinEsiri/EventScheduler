using EventScheduler.Application.DTOs.Response;
using EventScheduler.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventScheduler.Web.Components.Pages;

public partial class PublicEvents : IAsyncDisposable
{
    [Inject] private ApiService ApiService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<PublicEvents> Logger { get; set; } = default!;
    [Inject] private EventUIHelperService UIHelper { get; set; } = default!;
    [Inject] private NetworkStatusService NetworkStatusService { get; set; } = default!;

    private List<EventResponse> events = new();
    private List<EventResponse> filteredEvents = new();
    private List<EventResponse> userEvents = new(); // User's personal events for checking joined status
    private EventResponse? selectedEvent = null;
    private bool isLoading = true;
    private bool isAuthenticated = false;
    private bool isProcessing = false;
    private string selectedEventType = "";
    private string searchQuery = "";
    private ViewMode viewMode = ViewMode.Calendar;
    private DotNetObjectReference<PublicEvents>? dotNetHelper;
    private bool calendarInitialized = false;
    private bool initializationAttempted = false;
    private int currentUserId = 0;
    private bool isOnline = true;
    
    private HubConnection? hubConnection;
    private readonly HashSet<int> recentlyProcessedEventIds = new();

    public enum ViewMode
    {
        List,
        Calendar
    }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("PublicEvents: Component initializing");
        
        await NetworkStatusService.InitializeAsync();
        isOnline = NetworkStatusService.IsOnline;
        NetworkStatusService.OnStatusChanged += async (online) =>
        {
            isOnline = online;
            await InvokeAsync(StateHasChanged);
        };
        
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        
        if (isAuthenticated)
        {
            var userIdClaim = authState.User.FindFirst("userId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                currentUserId = userId;
            }
        }
        
        // Load data and setup SignalR in parallel
        var loadTasks = new List<Task> { LoadPublicEvents(), InitializeSignalR() };
        
        // If authenticated, also load user's events to check joined status
        if (isAuthenticated)
        {
            loadTasks.Add(LoadUserEvents());
        }
        
        await Task.WhenAll(loadTasks);
        
        Logger.LogInformation("PublicEvents: Initialization complete, loading={IsLoading}, events={Count}", isLoading, events.Count);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Logger.LogInformation("PublicEvents: OnAfterRenderAsync called - firstRender={FirstRender}, viewMode={ViewMode}, calendarInitialized={CalendarInitialized}, isLoading={IsLoading}", 
            firstRender, viewMode, calendarInitialized, isLoading);
        
        // ONLY initialize on first render after data loads
        // This prevents re-initialization on every render
        if (firstRender && viewMode == ViewMode.Calendar && !calendarInitialized && !isLoading)
        {
            Logger.LogInformation("PublicEvents: Conditions met for calendar initialization");
            await InitializeCalendar();
        }
        else
        {
            Logger.LogInformation("PublicEvents: Skipping calendar init - conditions not met");
        }
    }

    private async Task LoadPublicEvents()
    {
        isLoading = true;
        Logger.LogInformation("PublicEvents: Loading events from API");
        
        try
        {
            var response = await ApiService.GetPublicEventsAsync();
            if (response != null)
            {
                events = response;
                filteredEvents = events;
                Logger.LogInformation("PublicEvents: Loaded {Count} events", events.Count);
            }
            else
            {
                Logger.LogWarning("PublicEvents: API returned null response");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PublicEvents: Error loading events");
        }
        finally
        {
            isLoading = false;
            Logger.LogInformation("PublicEvents: Loading complete, isLoading={IsLoading}", isLoading);
            
            // If we're in calendar mode and haven't initialized yet, trigger initialization
            if (viewMode == ViewMode.Calendar && !calendarInitialized)
            {
                StateHasChanged(); // Trigger OnAfterRenderAsync with isLoading=false
                await Task.Delay(100); // Give time for render
                
                // Initialize directly if still not initialized
                if (!calendarInitialized)
                {
                    await InitializeCalendar();
                }
            }
        }
    }

    private async Task LoadUserEvents()
    {
        try
        {
            Logger.LogInformation("PublicEvents: Loading user's events");
            var response = await ApiService.GetAllEventsAsync();
            if (response != null)
            {
                userEvents = response;
                Logger.LogInformation("PublicEvents: Loaded {Count} user events", userEvents.Count);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PublicEvents: Error loading user events");
            userEvents = new List<EventResponse>();
        }
    }

    private async Task InitializeSignalR()
    {
        try
        {
            // Public events don't require authentication for viewing
            var hubUrl = "http://localhost:5006/hubs/events";
            
            hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .ConfigureLogging(logging => {
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            // Connection lifecycle handlers
            hubConnection.Reconnecting += OnReconnecting;
            hubConnection.Reconnected += OnReconnected;
            hubConnection.Closed += OnClosed;

            // Register event handlers for real-time updates
            RegisterSignalRHandlers();

            await hubConnection.StartAsync();
            
            Logger.LogInformation("PublicEvents SignalR: Connected (Connection ID: {ConnectionId})", hubConnection.ConnectionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PublicEvents SignalR: Connection failed");
        }
    }

    private void RegisterSignalRHandlers()
    {
        if (hubConnection == null) return;

        // Event created handler - add new public events
        hubConnection.On<EventResponse>("EventCreated", async (eventData) => {
            await InvokeAsync(async () => {
                // Only add if it's a public event and we haven't already added it
                if (eventData.IsPublic && !events.Any(e => e.Id == eventData.Id))
                {
                    Logger.LogInformation("PublicEvents: New public event created - {Title}", eventData.Title);
                    events.Add(eventData);
                    
                    // Re-apply filters
                    FilterEvents();
                    
                    // Update calendar if in calendar view
                    if (viewMode == ViewMode.Calendar && calendarInitialized)
                    {
                        await JSRuntime.InvokeVoidAsync("addEventToCalendar", eventData);
                    }
                    
                    StateHasChanged();
                }
                else if (eventData.IsPublic && events.Any(e => e.Id == eventData.Id))
                {
                    Logger.LogInformation("PublicEvents: Event {EventId} already exists, skipping duplicate", eventData.Id);
                }
            });
        });

        // Event updated handler
        hubConnection.On<EventResponse>("EventUpdated", async (eventData) => {
            await InvokeAsync(async () => {
                var existingEvent = events.FirstOrDefault(e => e.Id == eventData.Id);
                
                // Handle visibility changes
                if (existingEvent != null && !eventData.IsPublic)
                {
                    // Event was made private - remove it
                    Logger.LogInformation("PublicEvents: Event {Title} made private - removing", eventData.Title);
                    events.Remove(existingEvent);
                    FilterEvents();
                    
                    if (viewMode == ViewMode.Calendar && calendarInitialized)
                    {
                        await JSRuntime.InvokeVoidAsync("removeEventFromCalendar", eventData.Id);
                    }
                }
                else if (existingEvent != null && eventData.IsPublic)
                {
                    // Event updated and still public
                    Logger.LogInformation("PublicEvents: Public event updated - {Title}", eventData.Title);
                    events.Remove(existingEvent);
                    events.Add(eventData);
                    FilterEvents();
                    
                    if (viewMode == ViewMode.Calendar && calendarInitialized)
                    {
                        await JSRuntime.InvokeVoidAsync("updateEventInCalendar", eventData);
                    }
                }
                else if (existingEvent == null && eventData.IsPublic)
                {
                    // Private event was made public - add it
                    Logger.LogInformation("PublicEvents: Event {Title} made public - adding", eventData.Title);
                    events.Add(eventData);
                    FilterEvents();
                    
                    if (viewMode == ViewMode.Calendar && calendarInitialized)
                    {
                        await JSRuntime.InvokeVoidAsync("addEventToCalendar", eventData);
                    }
                }
                
                StateHasChanged();
            });
        });

        // Event deleted handler
        hubConnection.On<object>("EventDeleted", async (deletedEventInfo) => {
            await InvokeAsync(async () => {
                var eventId = ExtractEventId(deletedEventInfo);
                var eventToRemove = events.FirstOrDefault(e => e.Id == eventId);
                
                if (eventToRemove != null)
                {
                    Logger.LogInformation("PublicEvents: Event deleted - {Title}", eventToRemove.Title);
                    events.Remove(eventToRemove);
                    FilterEvents();
                    
                    if (viewMode == ViewMode.Calendar && calendarInitialized)
                    {
                        await JSRuntime.InvokeVoidAsync("removeEventFromCalendar", eventId);
                    }
                    
                    StateHasChanged();
                }
            });
        });
    }

    private int ExtractEventId(object deletedEventInfo)
    {
        var json = JsonSerializer.Serialize(deletedEventInfo);
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    private Task OnReconnecting(Exception? exception)
    {
        Logger.LogWarning(exception, "PublicEvents SignalR: ⚠️ Connection lost, attempting to reconnect...");
        InvokeAsync(StateHasChanged);
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        Logger.LogInformation("PublicEvents SignalR: ✓ Reconnected successfully (Connection ID: {ConnectionId})", connectionId);
        InvokeAsync(StateHasChanged);
        return Task.CompletedTask;
    }

    private Task OnClosed(Exception? exception)
    {
        Logger.LogWarning(exception, "PublicEvents SignalR: Connection closed");
        InvokeAsync(StateHasChanged);
        return Task.CompletedTask;
    }

    private async Task SetViewMode(ViewMode mode)
    {
        Logger.LogInformation("PublicEvents: Switching view mode from {PreviousMode} to {NewMode}", viewMode, mode);
        
        var previousMode = viewMode;
        viewMode = mode;
        
        if (mode == ViewMode.Calendar)
        {
            // Switching TO calendar view
            StateHasChanged();
            await Task.Delay(300); // Give DOM time to render calendar element
            
            // Initialize calendar if coming from list view or not yet initialized
            if (previousMode == ViewMode.List || !calendarInitialized)
            {
                await InitializeCalendar();
            }
        }
        else if (previousMode == ViewMode.Calendar && calendarInitialized)
        {
            // Switching FROM calendar view - destroy properly
            try
            {
                await JSRuntime.InvokeVoidAsync("fullCalendarInterop.destroy", "public-calendar");
                calendarInitialized = false;
                initializationAttempted = false; // Allow re-initialization when switching back
                Logger.LogInformation("PublicEvents: Calendar destroyed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "PublicEvents: Error destroying calendar");
                calendarInitialized = false;
                initializationAttempted = false;
            }
        }
    }

    private async Task InitializeCalendar()
    {
        // Prevent double initialization
        if (calendarInitialized || initializationAttempted)
        {
            Logger.LogInformation("PublicEvents: Calendar already initialized or attempt in progress, skipping");
            return;
        }

        initializationAttempted = true; // Mark that we're attempting initialization
        
        try
        {
            Logger.LogInformation("PublicEvents: Attempting to initialize calendar with {Count} events", filteredEvents.Count);
            
            // Ensure DOM element exists
            var elementExists = await JSRuntime.InvokeAsync<bool>("eval", "document.getElementById('public-calendar') !== null");
            if (!elementExists)
            {
                Logger.LogError("PublicEvents: Calendar element 'public-calendar' not found in DOM");
                initializationAttempted = false; // Reset flag to allow retry
                return;
            }
            
            // Check if there's an existing calendar instance and destroy it
            try
            {
                var hasExistingCalendar = await JSRuntime.InvokeAsync<bool>("eval", 
                    "window.fullCalendarInterop && window.fullCalendarInterop.calendars && window.fullCalendarInterop.calendars['public-calendar'] !== undefined");
                
                if (hasExistingCalendar)
                {
                    Logger.LogWarning("PublicEvents: Found existing calendar instance, destroying it first");
                    await JSRuntime.InvokeVoidAsync("fullCalendarInterop.destroy", "public-calendar");
                    await Task.Delay(100); // Give time for cleanup
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "PublicEvents: Error checking/destroying existing calendar");
            }
            
            dotNetHelper = DotNetObjectReference.Create(this);
            var calendarEvents = ConvertToFullCalendarFormat();
            
            // Give time for DOM to be fully ready and CSS to apply
            await Task.Delay(500);
            
            var initialized = await JSRuntime.InvokeAsync<bool>("fullCalendarInterop.initialize", 
                "public-calendar", dotNetHelper, calendarEvents, false);
            
            if (initialized)
            {
                calendarInitialized = true;
                Logger.LogInformation("PublicEvents: Calendar initialized successfully with {Count} events", calendarEvents.Length);
                
                // Force calendar to recalculate size after initialization
                await Task.Delay(100);
                await JSRuntime.InvokeVoidAsync("fullCalendarInterop.updateSize", "public-calendar");
                Logger.LogInformation("PublicEvents: Calendar size updated");
            }
            else
            {
                Logger.LogWarning("PublicEvents: Calendar initialization returned false");
                initializationAttempted = false; // Reset flag to allow retry
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PublicEvents: Error initializing calendar");
            initializationAttempted = false; // Reset flag to allow retry
        }
    }

    private object[] ConvertToFullCalendarFormat()
    {
        return filteredEvents.Select(e => new
        {
            id = e.Id,
            title = e.Title,
            start = e.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
            end = e.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
            allDay = e.IsAllDay,
            backgroundColor = "#667eea",
            borderColor = "#667eea",
            extendedProps = new
            {
                description = e.Description,
                location = e.Location,
                eventType = e.EventType
            }
        }).ToArray();
    }

    [JSInvokable]
    public Task OnEventClick(int eventId)
    {
        var evt = events.FirstOrDefault(e => e.Id == eventId);
        if (evt != null)
        {
            ShowEventDetails(evt);
            StateHasChanged(); // Force UI update to show modal
        }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnDateSelect(string startStr, string endStr, bool allDay)
    {
        // Public calendar is read-only, do nothing
        return Task.CompletedTask;
    }

    private void FilterEvents()
    {
        filteredEvents = events.Where(e =>
        {
            bool matchesType = string.IsNullOrEmpty(selectedEventType) || e.EventType == selectedEventType;
            bool matchesSearch = string.IsNullOrEmpty(searchQuery) ||
                                e.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                (e.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false);
            return matchesType && matchesSearch;
        }).ToList();

        // Update calendar if in calendar view
        if (viewMode == ViewMode.Calendar && calendarInitialized)
        {
            var calendarEvents = ConvertToFullCalendarFormat();
            JSRuntime.InvokeVoidAsync("fullCalendarInterop.updateEvents", calendarEvents);
        }
    }

    private void ShowEventDetails(EventResponse evt)
    {
        selectedEvent = evt;
        StateHasChanged(); // Force UI update to show modal
    }

    private void CloseModal()
    {
        selectedEvent = null;
        StateHasChanged(); // Force UI update to hide modal
    }

    private bool IsUserJoined(EventResponse evt)
    {
        if (!isAuthenticated || currentUserId == 0 || userEvents == null)
            return false;
        
        // Check if the user has a copy of this public event (joined event with matching OriginalEventId)
        return userEvents.Any(e => e.IsJoinedEvent && e.OriginalEventId == evt.Id);
    }

    private async Task JoinEvent(int eventId)
    {
        isProcessing = true;
        StateHasChanged();

        try
        {
            var joinedEventCopy = await ApiService.JoinPublicEventAsync(eventId);
            if (joinedEventCopy != null)
            {
                // Reload user events to update joined status
                await LoadUserEvents();
                
                // Update selected event to close modal and show joined status
                if (selectedEvent?.Id == eventId)
                {
                    StateHasChanged(); // Update UI to reflect joined status
                }

                Logger.LogInformation("Successfully joined event {EventId}", eventId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error joining event {EventId}", eventId);
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task LeaveEvent(int eventId)
    {
        isProcessing = true;
        StateHasChanged();

        try
        {
            await ApiService.LeaveEventAsync(eventId);
            
            // Reload user events to update joined status
            await LoadUserEvents();
            
            // Close the modal
            CloseModal();
            
            Logger.LogInformation("Successfully left event {EventId}", eventId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error leaving event {EventId}", eventId);
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (calendarInitialized)
            {
                await JSRuntime.InvokeVoidAsync("fullCalendarInterop.destroy", "public-calendar");
                calendarInitialized = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing calendar");
        }

        try
        {
            if (hubConnection != null)
            {
                await hubConnection.StopAsync();
                await hubConnection.DisposeAsync();
                Logger.LogInformation("PublicEvents SignalR: Connection disposed");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing SignalR connection");
        }

        dotNetHelper?.Dispose();
    }

    private void RedirectToLogin()
    {
        NavigationManager.NavigateTo("/login");
    }
}
