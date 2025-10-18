using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using EventScheduler.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace EventScheduler.Web.Components.Pages;

/// <summary>
/// Calendar List page with full offline support
/// Displays events in a list view with filtering and search capabilities
/// Uses offline-first architecture with automatic sync when online
/// </summary>
public partial class CalendarList : IAsyncDisposable
{
    [Inject] private ApiService ApiService { get; set; } = default!;
    [Inject] private OfflineEventService OfflineEventService { get; set; } = default!;
    [Inject] private ConnectivityService ConnectivityService { get; set; } = default!;
    [Inject] private SyncService SyncService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private EventUIHelperService UIHelper { get; set; } = default!;
    [Inject] private ILogger<CalendarList> Logger { get; set; } = default!;

    // State management
    private List<EventResponse> events = new();
    private List<EventResponse> filteredEvents = new();
    private bool isLoading = true;
    private bool showModal = false;
    private bool isEditMode = false;
    private int editEventId = 0;
    private CreateEventRequest eventRequest = new() { Title = "", StartDate = DateTime.Now, EndDate = DateTime.Now.AddHours(1) };
    private bool hasCheckedAuth = false;
    
    // Filtering and search
    private string selectedEventType = "";
    private string selectedStatus = "";
    private string searchQuery = "";
    private TabType activeTab = TabType.Active;
    
    // Connectivity state
    private bool isConnected = false;
    private string? connectionStatus;

    public enum TabType
    {
        Active,
        History
    }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("CalendarList: Initializing component");
        
        // Initialize connectivity service
        try
        {
            await ConnectivityService.InitializeAsync();
            isConnected = ConnectivityService.IsOnline;
            connectionStatus = isConnected ? "Connected" : "Offline";
            
            // Subscribe to connectivity changes
            ConnectivityService.ConnectivityChanged += OnConnectivityChanged;
            
            // Subscribe to sync events
            SyncService.SyncStarted += OnSyncStarted;
            SyncService.SyncCompleted += OnSyncCompleted;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarList: Failed to initialize connectivity service");
        }
        
        // Try to check auth, but don't redirect here - wait for OnAfterRenderAsync
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            Logger.LogInformation("CalendarList: User authenticated in memory");
            hasCheckedAuth = true;
            
            var token = user.FindFirst("token")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                ApiService.SetToken(token);
                await LoadEvents();
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !hasCheckedAuth)
        {
            hasCheckedAuth = true;
            Logger.LogInformation("CalendarList: First render - checking authentication");
            
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            
            if (user.Identity?.IsAuthenticated != true)
            {
                NavigationManager.NavigateTo("/login", forceLoad: true);
                return;
            }
            
            var token = user.FindFirst("token")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                ApiService.SetToken(token);
                await LoadEvents();
                StateHasChanged();
            }
            else
            {
                NavigationManager.NavigateTo("/login", forceLoad: true);
                return;
            }
        }
    }

    /// <summary>
    /// Load events using offline-first approach
    /// Attempts to load from server when online, falls back to local cache when offline
    /// </summary>
    private async Task LoadEvents()
    {
        try
        {
            isLoading = true;
            Logger.LogInformation("CalendarList: Loading events (connected: {IsConnected})", isConnected);
            
            // Use offline-first service for data retrieval
            events = await OfflineEventService.GetEventsAsync();
            FilterEvents();
            
            Logger.LogInformation("CalendarList: Loaded {Count} events", events.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarList: Failed to load events");
            NavigationManager.NavigateTo("/login");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void FilterEvents()
    {
        filteredEvents = events.Where(e =>
        {
            // Filter by tab - Active shows Scheduled, InProgress, Late; History shows Completed, Cancelled
            bool matchesTab = activeTab == TabType.Active 
                ? (e.Status == "Scheduled" || e.Status == "InProgress" || e.Status == "Late")
                : (e.Status == "Completed" || e.Status == "Cancelled");
            
            bool matchesType = string.IsNullOrEmpty(selectedEventType) || e.EventType == selectedEventType;
            bool matchesStatus = string.IsNullOrEmpty(selectedStatus) || e.Status == selectedStatus;
            bool matchesSearch = string.IsNullOrEmpty(searchQuery) ||
                                e.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                (e.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false);
            return matchesTab && matchesType && matchesStatus && matchesSearch;
        }).ToList();
    }

    private void SetActiveTab(TabType tab)
    {
        activeTab = tab;
        FilterEvents();
        StateHasChanged();
    }

    private void ShowCreateModal()
    {
        isEditMode = false;
        eventRequest = new CreateEventRequest 
        { 
            Title = "", 
            StartDate = DateTime.Now, 
            EndDate = DateTime.Now.AddHours(1) 
        };
        showModal = true;
    }

    private void ShowEditModal(EventResponse evt)
    {
        isEditMode = true;
        editEventId = evt.Id;
        eventRequest = new CreateEventRequest
        {
            Title = evt.Title,
            Description = evt.Description,
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            Location = evt.Location,
            IsAllDay = evt.IsAllDay,
            Color = evt.Color,
            CategoryId = evt.CategoryId,
            EventType = evt.EventType,
            IsPublic = evt.IsPublic,
            Invitations = evt.Invitations?.Select(i => new EventInvitationRequest 
            { 
                InviteeName = i.InviteeName, 
                InviteeEmail = i.InviteeEmail 
            }).ToList()
        };
        showModal = true;
    }

    private void CloseModal()
    {
        showModal = false;
    }

    private void AddInvitation()
    {
        if (eventRequest.Invitations == null)
        {
            eventRequest.Invitations = new List<EventInvitationRequest>();
        }
        eventRequest.Invitations.Add(new EventInvitationRequest { InviteeName = "", InviteeEmail = "" });
    }

    /// <summary>
    /// Save event (create or update) using offline-first approach
    /// </summary>
    private async Task SaveEvent()
    {
        try
        {
            Logger.LogInformation("CalendarList: Saving event (edit mode: {IsEdit})", isEditMode);
            
            if (isEditMode)
            {
                var updateRequest = new UpdateEventRequest
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
                    Invitations = eventRequest.Invitations?.Where(i => !string.IsNullOrWhiteSpace(i.InviteeName) && !string.IsNullOrWhiteSpace(i.InviteeEmail)).ToList()
                };
                
                // Use offline-first service
                await OfflineEventService.UpdateEventAsync(editEventId, updateRequest);
            }
            else
            {
                // Clean up empty invitations before sending
                if (eventRequest.Invitations != null)
                {
                    eventRequest.Invitations = eventRequest.Invitations
                        .Where(i => !string.IsNullOrWhiteSpace(i.InviteeName) && !string.IsNullOrWhiteSpace(i.InviteeEmail))
                        .ToList();
                }
                
                // Use offline-first service
                await OfflineEventService.CreateEventAsync(eventRequest);
            }

            CloseModal();
            await LoadEvents();
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "CalendarList: Validation error saving event");
            await JSRuntime.InvokeVoidAsync("alert", ex.Message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarList: Failed to save event");
            var message = isEditMode ? "Failed to update event." : "Failed to create event.";
            await JSRuntime.InvokeVoidAsync("alert", message);
        }
    }

    /// <summary>
    /// Delete event using offline-first approach
    /// </summary>
    private async Task DeleteEvent(int id)
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this event?");
        if (confirmed)
        {
            try
            {
                Logger.LogInformation("CalendarList: Deleting event {EventId}", id);
                
                // Use offline-first service
                await OfflineEventService.DeleteEventAsync(id);
                await LoadEvents();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CalendarList: Error deleting event {EventId}", id);
                await JSRuntime.InvokeVoidAsync("alert", "Failed to delete event. Please try again.");
            }
        }
    }

    /// <summary>
    /// Handle connectivity state changes
    /// </summary>
    private void OnConnectivityChanged(object? sender, bool isOnline)
    {
        isConnected = isOnline;
        connectionStatus = isOnline ? "Connected" : "Offline";
        
        Logger.LogInformation("CalendarList: Connectivity changed to {Status}", connectionStatus);
        
        InvokeAsync(async () =>
        {
            if (isOnline)
            {
                // Reload events from server when coming back online
                await LoadEvents();
            }
            StateHasChanged();
        });
    }

    /// <summary>
    /// Handle sync started event
    /// </summary>
    private void OnSyncStarted(object? sender, EventArgs e)
    {
        connectionStatus = "Syncing";
        InvokeAsync(() => StateHasChanged());
    }

    /// <summary>
    /// Handle sync completed event
    /// </summary>
    private void OnSyncCompleted(object? sender, SyncResult result)
    {
        connectionStatus = isConnected ? "Connected" : "Offline";
        
        InvokeAsync(async () =>
        {
            if (result.Success)
            {
                // Reload events after successful sync
                await LoadEvents();
            }
            StateHasChanged();
        });
    }

    /// <summary>
    /// Cleanup resources and unsubscribe from events
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        ConnectivityService.ConnectivityChanged -= OnConnectivityChanged;
        SyncService.SyncStarted -= OnSyncStarted;
        SyncService.SyncCompleted -= OnSyncCompleted;
        
        try
        {
            await ConnectivityService.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "CalendarList: Error disposing connectivity service");
        }
    }
}
