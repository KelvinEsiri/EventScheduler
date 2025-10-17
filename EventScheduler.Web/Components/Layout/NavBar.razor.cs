using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace EventScheduler.Web.Components.Layout;

public partial class NavBar : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private bool isAuthenticated = false;
    private string userName = string.Empty;
    private bool hasCheckedAuth = false;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to auth state changes
        AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        
        // Try to get auth state (might not work during prerendering)
        await CheckAuthenticationState();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !hasCheckedAuth)
        {
            hasCheckedAuth = true;
            
            try
            {
                await JSRuntime.InvokeVoidAsync("console.log", "[NavBar] OnAfterRenderAsync - checking auth after JS available");
            }
            catch { /* JS not available */ }
            
            // Small delay to ensure AuthStateProvider has loaded from localStorage
            await Task.Delay(100);
            
            // Check again after JS interop is available and AuthStateProvider has loaded
            await CheckAuthenticationState();
            
            try
            {
                await JSRuntime.InvokeVoidAsync("console.log", $"[NavBar] OnAfterRenderAsync - isAuthenticated: {isAuthenticated}, userName: {userName}");
            }
            catch { /* JS not available */ }
            
            StateHasChanged();
        }
    }

    private async Task CheckAuthenticationState()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        isAuthenticated = user.Identity?.IsAuthenticated ?? false;
        userName = user.Identity?.Name ?? string.Empty;
        
        if (isAuthenticated)
        {
            hasCheckedAuth = true;
        }
    }

    private void NavigateToPublicEvents()
    {
        // Only navigate if not already on public-events page
        var currentUri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).AbsolutePath;
        
        Console.WriteLine($"[NavBar] NavigateToPublicEvents called. Current URI: {currentUri}");
        
        if (currentUri != "/public-events")
        {
            Console.WriteLine($"[NavBar] Navigating to /public-events");
            NavigationManager.NavigateTo("/public-events");
        }
        else
        {
            Console.WriteLine($"[NavBar] Already on /public-events, ignoring navigation");
        }
    }

    private void NavigateToCalendar()
    {
        // Only navigate if not already on calendar-view page
        var currentUri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).AbsolutePath;
        
        Console.WriteLine($"[NavBar] NavigateToCalendar called. Current URI: {currentUri}");
        
        if (currentUri != "/calendar-view")
        {
            Console.WriteLine($"[NavBar] Navigating to /calendar-view");
            NavigationManager.NavigateTo("/calendar-view");
        }
        else
        {
            Console.WriteLine($"[NavBar] Already on /calendar-view, ignoring navigation");
        }
    }

    private void HandleLogout()
    {
        try
        {
            Console.WriteLine("[NavBar] Logout clicked, navigating to /logout");
            NavigationManager.NavigateTo("/logout");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NavBar] Error during logout navigation: {ex.Message}");
            // Force navigation even if error occurs
            NavigationManager.NavigateTo("/logout", forceLoad: true);
        }
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        InvokeAsync(async () =>
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("console.log", "[NavBar] OnAuthenticationStateChanged triggered");
            }
            catch { /* JS not available */ }
            
            var authState = await task;
            var user = authState.User;
            
            isAuthenticated = user.Identity?.IsAuthenticated ?? false;
            userName = user.Identity?.Name ?? string.Empty;
            
            try
            {
                await JSRuntime.InvokeVoidAsync("console.log", $"[NavBar] Auth changed - IsAuth: {isAuthenticated}, UserName: {userName}");
            }
            catch { /* JS not available */ }
            
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}
