using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using EventScheduler.Web.Services;

namespace EventScheduler.Web.Components.Pages;

public partial class Logout
{
    [Inject] private AuthStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private ApiService ApiService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<Logout> Logger { get; set; } = default!;

    private bool hasLoggedOut = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !hasLoggedOut)
        {
            hasLoggedOut = true;
            
            try
            {
                Logger.LogInformation("Logout: Starting logout process");
                
                // Clear authentication state (includes localStorage via JSInterop)
                await AuthStateProvider.ClearAuthentication();
                Logger.LogInformation("Logout: Authentication state and localStorage cleared");
                
                // Clear API token
                ApiService.ClearToken();
                Logger.LogInformation("Logout: API token cleared");
                
                // Brief delay for better UX
                await Task.Delay(500);
                
                Logger.LogInformation("Logout: Redirecting to login page");
                
                // Force a full page reload to ensure all Blazor circuit state is cleared
                NavigationManager.NavigateTo("/login", forceLoad: true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Logout: Error during logout process - {Message}", ex.Message);
                
                // Still redirect even if there's an error
                try
                {
                    // Try to clear at least the in-memory state
                    ApiService.ClearToken();
                }
                catch { /* Ignore */ }
                
                Logger.LogInformation("Logout: Forcing redirect to login despite errors");
                NavigationManager.NavigateTo("/login", forceLoad: true);
            }
        }
    }
}
