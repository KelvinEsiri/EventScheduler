using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using EventScheduler.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace EventScheduler.Web.Components.Pages;

public partial class CalendarList
{
    [Inject] private ApiService ApiService { get; set; } = default!;
    [Inject] private OfflineSyncService OfflineSyncService { get; set; } = default!;
    [Inject] private NetworkStatusService NetworkStatusService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private EventUIHelperService UIHelper { get; set; } = default!;

    private List<EventResponse> events = new();
    private List<EventResponse> filteredEvents = new();
    private bool isLoading = true;
    private bool showModal = false;
    private bool isEditMode = false;
    private int editEventId = 0;
    private CreateEventRequest eventRequest = new() { Title = "", StartDate = DateTime.Now, EndDate = DateTime.Now.AddHours(1) };
    private bool hasCheckedAuth = false;
    private string selectedEventType = "";
    private string selectedStatus = "";
    private string searchQuery = "";
    private TabType activeTab = TabType.Active;
    private bool isOnline = true;
    private int pendingOperationsCount = 0;

    public enum TabType
    {
        Active,
        History
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !hasCheckedAuth)
        {
            hasCheckedAuth = true;
            
            // Now JavaScript is available and AuthStateProvider can load from localStorage
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            
            if (user.Identity?.IsAuthenticated != true)
            {
                NavigationManager.NavigateTo("/login", forceLoad: true);
                return;
            }
            
            // Get token from auth state and set it in ApiService
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

    protected override async Task OnInitializedAsync()
    {
        // Initialize offline sync service
        await OfflineSyncService.InitializeAsync();
        isOnline = NetworkStatusService.IsOnline;
        NetworkStatusService.OnStatusChanged += async (online) => 
        {
            isOnline = online;
            await InvokeAsync(StateHasChanged);
        };
        OfflineSyncService.OnPendingOperationsCountChanged += async (count) =>
        {
            pendingOperationsCount = count;
            await InvokeAsync(StateHasChanged);
        };
        
        pendingOperationsCount = await OfflineSyncService.GetPendingOperationsCountAsync();
        
        // Try to check auth, but don't redirect here - wait for OnAfterRenderAsync
        // because during prerendering, localStorage hasn't been read yet
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            hasCheckedAuth = true;
            
            // Set API token
            var token = user.FindFirst("token")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                ApiService.SetToken(token);
                await LoadEvents();
            }
        }
        else
        {
            // Don't redirect here - wait for OnAfterRenderAsync when localStorage is available
        }
    }

    private async Task LoadEvents()
    {
        try
        {
            isLoading = true;
            // Use OfflineSyncService which handles online/offline automatically
            events = await OfflineSyncService.LoadEventsAsync();
            FilterEvents();
        }
        catch (Exception)
        {
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

    private async Task SaveEvent()
    {
        try
        {
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
                
                if (isOnline)
                {
                    await ApiService.UpdateEventAsync(editEventId, updateRequest);
                }
                else
                {
                    await OfflineSyncService.UpdateEventOfflineAsync(editEventId, updateRequest);
                }
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
                
                if (isOnline)
                {
                    await ApiService.CreateEventAsync(eventRequest);
                }
                else
                {
                    await OfflineSyncService.CreateEventOfflineAsync(eventRequest);
                }
            }

            CloseModal();
            await LoadEvents();
            
            if (!isOnline)
            {
                await JSRuntime.InvokeVoidAsync("alert", "Changes saved offline and will sync when you're back online.");
            }
        }
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
    }

    private async Task DeleteEvent(int id)
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this event?");
        if (confirmed)
        {
            try
            {
                if (isOnline)
                {
                    await ApiService.DeleteEventAsync(id);
                }
                else
                {
                    await OfflineSyncService.DeleteEventOfflineAsync(id);
                }
                
                await LoadEvents();
                
                if (!isOnline)
                {
                    await JSRuntime.InvokeVoidAsync("alert", "Event deleted offline and will sync when you're back online.");
                }
            }
            catch (Exception ex)
            {
                // Handle error - could add error message display
                Console.WriteLine($"Error deleting event: {ex.Message}");
            }
        }
    }
}
