using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace EventScheduler.Web.Services;

/// <summary>
/// Circuit-scoped authentication state provider
/// Manages authentication state for the current user session following NasosoTax reference patterns
/// </summary>
public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    private ClaimsPrincipal? _currentUser;
    private bool _initialized = false;
    private string? _token;
    private string? _username;
    private int? _userId;

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

    /// <summary>
    /// Marks the user as authenticated and stores credentials in memory
    /// Called after successful login
    /// </summary>
    public void SetAuthentication(string username, string email, int userId, string token)
    {
        _username = username;
        _userId = userId;
        _token = token;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("userId", userId.ToString()),
            new Claim("token", token)
        };

        var identity = new ClaimsIdentity(claims, "apiauth");
        _currentUser = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private void SetAuthenticationInternal(string username, string email, int userId, string token)
    {
        _username = username;
        _userId = userId;
        _token = token;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("userId", userId.ToString()),
            new Claim("token", token)
        };

        var identity = new ClaimsIdentity(claims, "apiauth");
        _currentUser = new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Clears authentication state and marks user as logged out
    /// Called during logout or session expiration
    /// </summary>
    public async Task ClearAuthentication()
    {
        _username = null;
        _userId = null;
        _token = null;
        _currentUser = null;
        
        await _jsRuntime.InvokeVoidAsync("authHelper.clearAuthData");
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Checks if the current user is authenticated
    /// Used by protected pages to verify authentication before loading
    /// </summary>
    public bool IsAuthenticated()
    {
        return _currentUser?.Identity?.IsAuthenticated ?? false;
    }

    /// <summary>
    /// Gets the stored JWT token for API requests
    /// </summary>
    public string? GetToken()
    {
        return _token ?? _currentUser?.FindFirst("token")?.Value;
    }

    /// <summary>
    /// Gets the current username
    /// </summary>
    public string? GetUsername()
    {
        return _username ?? _currentUser?.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Gets the current user ID
    /// </summary>
    public int? GetUserId()
    {
        if (_userId.HasValue)
            return _userId.Value;
            
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
