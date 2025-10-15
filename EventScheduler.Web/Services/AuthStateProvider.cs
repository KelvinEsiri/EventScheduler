using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace EventScheduler.Web.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    private ClaimsPrincipal? _currentUser;
    private bool _initialized = false;

    public AuthStateProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_initialized)
        {
            await LoadAuthFromStorage();
            _initialized = true;
        }
        return new AuthenticationState(_currentUser ?? _anonymous);
    }
    
    private async Task LoadAuthFromStorage()
    {
        try
        {
            // Try to load from localStorage, but handle the case where JS interop isn't ready
            var authData = await _jsRuntime.InvokeAsync<AuthData>("authHelper.getAuthData");
            
            if (!string.IsNullOrEmpty(authData.Token) && !string.IsNullOrEmpty(authData.UserId))
            {
                SetAuthenticationInternal(authData.Username, authData.Email, int.Parse(authData.UserId), authData.Token);
            }
        }
        catch (InvalidOperationException)
        {
            // JS interop not available yet (during prerendering), authentication will be set on next call
        }
        catch (Exception)
        {
            // Ignore other errors loading from storage
        }
    }

    public void SetAuthentication(string username, string email, int userId, string token)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim("userId", userId.ToString()),
            new Claim("token", token)
        };

        var identity = new ClaimsIdentity(claims, "apiauth");
        _currentUser = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private void SetAuthenticationInternal(string username, string email, int userId, string token)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim("userId", userId.ToString()),
            new Claim("token", token)
        };

        var identity = new ClaimsIdentity(claims, "apiauth");
        _currentUser = new ClaimsPrincipal(identity);
    }

    public async Task ClearAuthentication()
    {
        _currentUser = null;
        await _jsRuntime.InvokeVoidAsync("authHelper.clearAuthData");
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public string? GetToken()
    {
        return _currentUser?.FindFirst("token")?.Value;
    }

    public int? GetUserId()
    {
        var userIdClaim = _currentUser?.FindFirst("userId")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public class AuthData
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Token { get; set; } = "";
}
