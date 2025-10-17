using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace EventScheduler.Web.Services;

/// <summary>
/// Circuit-scoped authentication state provider for Blazor Server.
/// Maintains authentication state within a single SignalR circuit (user session).
/// Uses AuthStateCache (singleton) to persist state across circuit changes.
/// Implements the three-tier persistence strategy:
/// 1. ASP.NET Session (browser cookie - Session ID)
/// 2. Singleton Cache (server memory - Auth data)
/// 3. Auto-restore on circuit creation (constructor)
/// </summary>
public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly ILogger<AuthStateProvider> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthStateCache _authStateCache;
    private readonly IJSRuntime _jsRuntime;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    private ClaimsPrincipal? _currentUser;
    private bool _initialized = false;
    private string? _token;
    private string? _username;
    private string? _email;
    private int? _userId;
    private readonly string _instanceId = Guid.NewGuid().ToString()[..8];
    private const string SessionIdKey = "EventSchedulerAuthSessionId";

    public AuthStateProvider(
        ILogger<AuthStateProvider> logger,
        IHttpContextAccessor httpContextAccessor,
        AuthStateCache authStateCache,
        IJSRuntime jsRuntime)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _authStateCache = authStateCache;
        _jsRuntime = jsRuntime;
        
        _logger.LogInformation("[AuthStateProvider] Instance created: {InstanceId}", _instanceId);
        
        // ⚡ CRITICAL: Try to restore authentication state from cache immediately
        // This runs when new circuit is created after reconnection
        RestoreFromCache();
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_initialized)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "[AuthStateProvider] GetAuthenticationStateAsync called - loading auth...");
                await LoadAuthFromStorage();
            }
            catch (Exception ex)
            {
                try 
                { 
                    await _jsRuntime.InvokeVoidAsync("console.log", $"[AuthStateProvider] Error in GetAuthenticationStateAsync: {ex.Message}");
                }
                catch { /* Ignore if JS not available */ }
            }
            finally
            {
                _initialized = true;
            }
        }
        
        var isAuth = _currentUser?.Identity?.IsAuthenticated ?? false;
        try
        {
            await _jsRuntime.InvokeVoidAsync("console.log", $"[AuthStateProvider] Returning auth state - IsAuthenticated: {isAuth}");
        }
        catch { /* Ignore if JS not available */ }
        
        return new AuthenticationState(_currentUser ?? _anonymous);
    }

    /// <summary>
    /// Gets or creates a session ID that persists across page reloads via browser cookie
    /// </summary>
    private string GetOrCreateSessionId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogDebug("[AuthStateProvider] No HTTP context available for instance {InstanceId}", _instanceId);
            return string.Empty;
        }

        // Try to get existing session ID from ASP.NET session (stored in cookie)
        var sessionId = httpContext.Session.GetString(SessionIdKey);
        if (string.IsNullOrEmpty(sessionId))
        {
            // Create a new session ID
            sessionId = Guid.NewGuid().ToString();
            httpContext.Session.SetString(SessionIdKey, sessionId);
            _logger.LogInformation(
                "[AuthStateProvider] Created new session ID: {SessionId} for instance {InstanceId}", 
                sessionId, _instanceId
            );
        }
        else
        {
            _logger.LogDebug(
                "[AuthStateProvider] Retrieved existing session ID: {SessionId} for instance {InstanceId}", 
                sessionId, _instanceId
            );
        }
        
        return sessionId;
    }

    /// <summary>
    /// Restores authentication state from singleton cache using session ID
    /// Called automatically when AuthStateProvider is instantiated (constructor)
    /// This is the key to reconnection persistence
    /// </summary>
    private void RestoreFromCache()
    {
        try
        {
            var sessionId = GetOrCreateSessionId();
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogDebug(
                    "[AuthStateProvider] No session ID available for instance {InstanceId}", 
                    _instanceId
                );
                return;
            }

            var cachedAuth = _authStateCache.GetAuthState(sessionId);
            if (cachedAuth != null)
            {
                // Restore authentication from cache
                _username = cachedAuth.Username;
                _email = cachedAuth.Email;
                _userId = cachedAuth.UserId;
                _token = cachedAuth.Token;

                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, cachedAuth.Username),
                    new Claim(ClaimTypes.Email, cachedAuth.Email),
                    new Claim(ClaimTypes.NameIdentifier, cachedAuth.UserId.ToString()),
                    new Claim("userId", cachedAuth.UserId.ToString()),
                    new Claim("token", cachedAuth.Token),
                }, "apiauth");

                _currentUser = new ClaimsPrincipal(identity);
                
                _logger.LogInformation(
                    "[AuthStateProvider] Authentication state restored from cache in instance {InstanceId}: {Username} (UserId: {UserId})", 
                    _instanceId, cachedAuth.Username, cachedAuth.UserId
                );
            }
            else
            {
                _logger.LogDebug(
                    "[AuthStateProvider] No cached auth found for session {SessionId} in instance {InstanceId}",
                    sessionId, _instanceId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, 
                "[AuthStateProvider] Failed to restore authentication state from cache in instance {InstanceId}", 
                _instanceId
            );
        }
    }
    
    private async Task LoadAuthFromStorage()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("console.log", "[AuthStateProvider] Loading auth from localStorage...");
            
            // Try to load from localStorage as fallback
            var authData = await _jsRuntime.InvokeAsync<AuthData>("authHelper.getAuthData");
            
            await _jsRuntime.InvokeVoidAsync("console.log", $"[AuthStateProvider] Auth data retrieved - Token: {(!string.IsNullOrEmpty(authData.Token) ? "Present" : "Missing")}, UserId: {authData.UserId}, Username: {authData.Username}");
            
            if (!string.IsNullOrEmpty(authData.Token) && !string.IsNullOrEmpty(authData.UserId))
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"[AuthStateProvider] Setting authentication for user: {authData.Username}");
                SetAuthenticationInternal(authData.Username, authData.Email, int.Parse(authData.UserId), authData.Token);
                
                // Also store in cache for reconnection persistence
                SaveToCache();
                
                // CRITICAL: Notify all components that auth state changed
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                await _jsRuntime.InvokeVoidAsync("console.log", "[AuthStateProvider] Auth state change notification sent");
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "[AuthStateProvider] No valid auth data found in localStorage");
            }
        }
        catch (InvalidOperationException ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.log", $"[AuthStateProvider] JS interop not available yet (prerendering): {ex.Message}");
        }
        catch (Exception ex)
        {
            await _jsRuntime.InvokeVoidAsync("console.log", $"[AuthStateProvider] Error loading from storage: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves current authentication to singleton cache
    /// </summary>
    private void SaveToCache()
    {
        if (_username == null || _token == null || !_userId.HasValue)
        {
            return;
        }

        try
        {
            var sessionId = GetOrCreateSessionId();
            if (!string.IsNullOrEmpty(sessionId))
            {
                _authStateCache.SetAuthState(sessionId, _username, _email ?? "", _userId.Value, _token);
                _logger.LogInformation(
                    "[AuthStateProvider] Authentication state saved to cache with session ID {SessionId} in instance {InstanceId}: {Username} (UserId: {UserId})", 
                    sessionId, _instanceId, _username, _userId.Value
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, 
                "[AuthStateProvider] Failed to save authentication state to cache in instance {InstanceId}", 
                _instanceId
            );
        }
    }

    /// <summary>
    /// Marks the user as authenticated and stores credentials
    /// Called after successful login
    /// Saves to both memory and singleton cache for reconnection persistence
    /// </summary>
    public void SetAuthentication(string username, string email, int userId, string token)
    {
        _username = username;
        _email = email;
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

        // Save to singleton cache for persistence across page navigations and reconnections
        SaveToCache();

        _logger.LogInformation(
            "[AuthStateProvider] User authenticated in instance {InstanceId}: {Username} (UserId: {UserId}), " +
            "Identity.IsAuthenticated: {IsAuthenticated}, AuthType: {AuthType}", 
            _instanceId, username, userId, identity.IsAuthenticated, identity.AuthenticationType
        );

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private void SetAuthenticationInternal(string username, string email, int userId, string token)
    {
        _username = username;
        _email = email;
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
    /// Clears from memory, localStorage, and singleton cache
    /// </summary>
    public async Task ClearAuthentication()
    {
        _username = null;
        _email = null;
        _userId = null;
        _token = null;
        _currentUser = null;
        
        // Clear from singleton cache
        try
        {
            var sessionId = GetOrCreateSessionId();
            if (!string.IsNullOrEmpty(sessionId))
            {
                _authStateCache.ClearAuthState(sessionId);
                _logger.LogInformation(
                    "[AuthStateProvider] Authentication state cleared from cache in instance {InstanceId}", 
                    _instanceId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, 
                "[AuthStateProvider] Failed to clear authentication state from cache in instance {InstanceId}", 
                _instanceId
            );
        }
        
        // Clear from localStorage
        try
        {
            await _jsRuntime.InvokeVoidAsync("authHelper.clearAuthData");
        }
        catch (InvalidOperationException ex)
        {
            // JSInterop not available (prerendering) - this is OK, we'll clear on client side
            await _jsRuntime.InvokeVoidAsync("console.log", $"[AuthStateProvider] JSInterop not available during logout: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Log but don't fail logout
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"[AuthStateProvider] Error clearing localStorage: {ex.Message}");
            }
            catch { /* Ignore if logging fails */ }
        }

        _logger.LogInformation("[AuthStateProvider] User logged out in instance {InstanceId}", _instanceId);
        
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Checks if the current user is authenticated
    /// Used by protected pages to verify authentication before loading
    /// CRITICAL: This is called after reconnection to determine if user should be redirected to login
    /// </summary>
    public bool IsAuthenticated()
    {
        var isAuth = _currentUser?.Identity?.IsAuthenticated ?? false;
        _logger.LogInformation(
            "[AuthStateProvider] IsAuthenticated check in instance {InstanceId}: {IsAuthenticated}, " +
            "Identity: {IdentityName}, AuthType: {AuthType}", 
            _instanceId,
            isAuth, 
            _currentUser?.Identity?.Name ?? "null",
            _currentUser?.Identity?.AuthenticationType ?? "null"
        );
        return isAuth;
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
    /// Gets the current user email
    /// </summary>
    public string? GetEmail()
    {
        return _email ?? _currentUser?.FindFirst(ClaimTypes.Email)?.Value;
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
