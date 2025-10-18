using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using EventScheduler.Web.Services;

namespace EventScheduler.Web.Components.Layout;

/// <summary>
/// Navigation menu with offline-aware link management
/// Greys out non-offline pages when connectivity is lost
/// </summary>
public partial class NavMenu : IAsyncDisposable
{
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ConnectivityService ConnectivityService { get; set; } = default!;
    [Inject] private ILogger<NavMenu> Logger { get; set; } = default!;

    private bool isOnline = true;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        
        // Initialize connectivity monitoring
        try
        {
            await ConnectivityService.InitializeAsync();
            isOnline = ConnectivityService.IsOnline;
            
            // Subscribe to connectivity changes
            ConnectivityService.ConnectivityChanged += OnConnectivityChanged;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "NavMenu: Failed to initialize connectivity service");
        }
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handle connectivity state changes
    /// </summary>
    private void OnConnectivityChanged(object? sender, bool online)
    {
        isOnline = online;
        Logger.LogInformation("NavMenu: Connectivity changed to {Status}", online ? "ONLINE" : "OFFLINE");
        InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        ConnectivityService.ConnectivityChanged -= OnConnectivityChanged;
        
        try
        {
            await ConnectivityService.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "NavMenu: Error disposing connectivity service");
        }
    }
}
