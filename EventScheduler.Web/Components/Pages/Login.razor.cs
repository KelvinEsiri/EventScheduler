using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using EventScheduler.Application.DTOs.Request;
using EventScheduler.Web.Services;

namespace EventScheduler.Web.Components.Pages;

public partial class Login
{
    [Inject] private ApiService ApiService { get; set; } = default!;
    [Inject] private AuthStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<Login> Logger { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [SupplyParameterFromForm]
    private LoginRequest loginRequest { get; set; } = new() { Username = "", Password = "" };
    
    private string? errorMessage;
    private bool isLoading = false;
    private bool shouldRedirect = false;
    private string? authUsername;
    private string? authEmail;
    private int authUserId;
    private string? authToken;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Logger.LogInformation("OnAfterRenderAsync called. FirstRender: {FirstRender}, ShouldRedirect: {ShouldRedirect}", firstRender, shouldRedirect);
        
        if (shouldRedirect && !string.IsNullOrEmpty(authToken))
        {
            // Reset flag to prevent multiple redirects
            shouldRedirect = false;
            
            try
            {
                Logger.LogInformation("Storing auth data in localStorage and redirecting");
                
                // Store in localStorage
                await JSRuntime.InvokeVoidAsync("authHelper.setAuthData", 
                    authUsername, authEmail, authUserId, authToken);
                
                // Wait a tiny bit to ensure localStorage write completes
                await Task.Delay(100);
                
                Logger.LogInformation("Auth data stored, now navigating with full page reload");
                
                // Force full page reload to ensure fresh circuit with auth loaded
                NavigationManager.NavigateTo("/calendar-view", forceLoad: true);
                
                Logger.LogInformation("Navigation command sent");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during redirect");
                errorMessage = $"Redirect failed: {ex.Message}";
                StateHasChanged();
            }
        }
    }

    private async Task HandleLogin()
    {
        // Prevent double submission
        if (isLoading) return;
        
        try
        {
            Logger.LogInformation("Login attempt started for user: {Username}", loginRequest.Username);
            isLoading = true;
            errorMessage = null;

            var response = await ApiService.LoginAsync(loginRequest);
            Logger.LogInformation("Login response received. Response is null: {IsNull}", response == null);
            
            if (response != null)
            {
                Logger.LogInformation("Setting authentication for user: {Username}", response.Username);
                
                // Store auth data temporarily
                authUsername = response.Username;
                authEmail = response.Email;
                authUserId = response.UserId;
                authToken = response.Token;
                
                // Set in memory for immediate use
                AuthStateProvider.SetAuthentication(response.Username, response.Email, response.UserId, response.Token);
                ApiService.SetToken(response.Token);
                
                // Set flag to redirect in OnAfterRenderAsync
                shouldRedirect = true;
                
                Logger.LogInformation("Triggering state change");
                StateHasChanged();
            }
            else
            {
                Logger.LogWarning("Login response was null");
                errorMessage = "Login failed. Please try again.";
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request exception during login");
            errorMessage = $"Unable to connect to server. Please make sure the API is running. Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during login");
            errorMessage = $"Login error: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            Logger.LogInformation("Login attempt completed. Loading: {IsLoading}", isLoading);
        }
    }
}
