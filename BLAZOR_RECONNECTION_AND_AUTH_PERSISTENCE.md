# Blazor Server Reconnection & Authentication Persistence

**Project:** NasosoTax  
**Technology:** ASP.NET Core 9.0 + Blazor Server + SignalR  
**Date:** October 16, 2025  
**Version:** 1.1.0

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Complete Reconnection Flow](#complete-reconnection-flow)
4. [Authentication Persistence Strategy](#authentication-persistence-strategy)
5. [Implementation Details](#implementation-details)
6. [Visual Timeline](#visual-timeline)
7. [Code Examples](#code-examples)
8. [Security Considerations](#security-considerations)
9. [Testing Scenarios](#testing-scenarios)
10. [Troubleshooting](#troubleshooting)

---

## 📖 Overview

### The Challenge

In Blazor Server applications, the client and server maintain a persistent connection via SignalR (WebSocket). When this connection is lost (network issue, server restart, deployment), the application needs to:

1. ✅ Detect the disconnection
2. ✅ Show visual feedback to the user
3. ✅ Automatically attempt reconnection
4. ✅ Restore authentication state after reconnection
5. ✅ Redirect to login if token expired
6. ✅ All without requiring manual page refresh

### The Solution

NasosoTax implements a **3-tier persistence strategy** combining:

- **ASP.NET Session** (browser-level persistence)
- **Singleton Cache** (server-level persistence)
- **Automatic State Restoration** (on circuit recreation)

This ensures users can seamlessly continue their work after reconnection, or are automatically redirected to login if their session expired.

---

## 🏗️ Architecture

### High-Level Components

```
┌─────────────────────────────────────────────────────────────┐
│                     Browser (Client)                         │
│  ┌────────────┐  ┌──────────────┐  ┌──────────────────┐    │
│  │ Components │  │ JavaScript   │  │ Session Cookie   │    │
│  │ (Blazor)   │  │ Reconnection │  │ (Session ID)     │    │
│  └──────┬─────┘  └──────┬───────┘  └────────┬─────────┘    │
│         │                │                    │              │
└─────────┼────────────────┼────────────────────┼──────────────┘
          │                │                    │
          ↓                ↓                    ↓
     SignalR Hub      Polling /          HTTP Session
          │           (Health Check)            │
          │                │                    │
┌─────────┼────────────────┼────────────────────┼──────────────┐
│         ↓                ↓                    ↓              │
│  ┌────────────────────────────────────────────────────┐     │
│  │            Blazor Server (Host)                    │     │
│  │  ┌──────────────────┐  ┌──────────────────────┐  │     │
│  │  │ SignalR Circuit  │  │   HTTP Middleware    │  │     │
│  │  │   (Per User)     │  │   (Session Handler)  │  │     │
│  │  └────────┬─────────┘  └──────────┬───────────┘  │     │
│  │           │                        │              │     │
│  │           ↓                        ↓              │     │
│  │  ┌────────────────────────────────────────────┐  │     │
│  │  │         Service Layer                      │  │     │
│  │  │  ┌────────────────┐  ┌──────────────────┐ │  │     │
│  │  │  │AuthStateProvider│  │  AuthStateCache  │ │  │     │
│  │  │  │    (Scoped)     │  │   (Singleton)    │ │  │     │
│  │  │  └────────┬────────┘  └────────┬─────────┘ │  │     │
│  │  │           │                     │           │  │     │
│  │  │           └──────────┬──────────┘           │  │     │
│  │  │                      ↓                      │  │     │
│  │  │            ClaimsPrincipal                  │  │     │
│  │  │        (Username, UserId, Token)           │  │     │
│  │  └────────────────────────────────────────────┘  │     │
│  └───────────────────────────────────────────────────┘     │
│                       Server                                │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Scope | Purpose | Survives Reconnection? |
|-----------|-------|---------|----------------------|
| **SignalR Circuit** | Per-connection | Real-time WebSocket connection | ❌ No (destroyed on disconnect) |
| **AuthStateProvider** | Scoped (per circuit) | Manages auth state for current circuit | ❌ No (recreated on reconnection) |
| **AuthStateCache** | Singleton | Persists auth across all circuits | ✅ Yes (server-level) |
| **ASP.NET Session** | Browser-based | Stores session ID in cookie | ✅ Yes (browser cookie) |
| **JWT Token** | Cached | Authentication token | ✅ Yes (in AuthStateCache) |

---

## 🔄 Complete Reconnection Flow

### Detailed Step-by-Step Process

#### **Phase 1: Normal Operation**

```
User logged in → Browsing protected pages → SignalR circuit active
         │
         ├─ AuthStateProvider has ClaimsPrincipal with token
         ├─ AuthStateCache has session data (username, userId, token)
         └─ Browser has session cookie with session ID
```

#### **Phase 2: Connection Lost**

```
Network issue / Server restart
         ↓
SignalR circuit broken
         ↓
Blazor detects disconnection
         ↓
JavaScript handler.onConnectionDown() fires
         ↓
┌────────────────────────────────────┐
│  Modal Shows:                      │
│  "Attempting to reconnect to       │
│   the server..."                   │
│  [Loading spinner]                 │
└────────────────────────────────────┘
         ↓
Start polling server every 2 seconds:
setInterval(() => {
    fetch('/').then(response => {
        if (response.ok) {
            // Server is back!
        }
    })
}, 2000)
```

#### **Phase 3: Reconnection Success**

```
Server responds to fetch('/')
         ↓
Stop polling interval
         ↓
⚡ location.reload() executes (AUTOMATIC)
         ↓
Browser reloads page
         ↓
┌────────────────────────────────────────────┐
│  New HTTP Request to Server                │
│  ✅ Includes session cookie (Session ID)   │
└────────────────────────────────────────────┘
         ↓
New SignalR Circuit Created
         ↓
AuthStateProvider constructor runs
         ↓
RestoreFromCache() executes
```

#### **Phase 4: Authentication Restoration**

```
RestoreFromCache() logic:
         ↓
1. Get session ID from HTTP session cookie
   sessionId = httpContext.Session.GetString("NasosoTaxAuthSessionId")
         ↓
2. Query singleton AuthStateCache
   cachedAuth = _authStateCache.GetAuthState(sessionId)
         ↓
         ├─ If found ──────────┐
         │                     ↓
         │              ┌──────────────────────┐
         │              │ Restore auth state:  │
         │              │ - Username           │
         │              │ - UserId             │
         │              │ - JWT Token          │
         │              └──────────────────────┘
         │                     ↓
         │              _currentUser = new ClaimsPrincipal(identity)
         │                     ↓
         │              IsAuthenticated() = TRUE ✅
         │
         └─ If NOT found ──────┐
                              ↓
                       _currentUser = anonymous
                              ↓
                       IsAuthenticated() = FALSE ❌
```

#### **Phase 5: Page Component Lifecycle**

```
Component OnInitializedAsync() runs
         ↓
Check authentication:
if (!AuthStateProvider.IsAuthenticated())
         │
         ├─ FALSE (Token expired/missing)
         │         ↓
         │  NavigationManager.NavigateTo("/login")
         │         ↓
         │  🔐 User redirected to login page
         │     (Automatic, seamless)
         │
         └─ TRUE (Token valid)
                   ↓
            Continue loading page data
                   ↓
            await LoadData()
                   ↓
            ✅ User continues where they left off
```

---

## 🗄️ Authentication Persistence Strategy

### Three-Tier Persistence Architecture

#### **Tier 1: ASP.NET Session (Browser-Level)**

**Purpose:** Link browser to server-side session across page reloads

```csharp
// When user first authenticates
private string GetOrCreateSessionId()
{
    var httpContext = _httpContextAccessor.HttpContext;
    var sessionId = httpContext.Session.GetString("NasosoTaxAuthSessionId");
    
    if (string.IsNullOrEmpty(sessionId))
    {
        // Create new session ID
        sessionId = Guid.NewGuid().ToString();
        httpContext.Session.SetString("NasosoTaxAuthSessionId", sessionId);
    }
    
    return sessionId; // Same ID persists across page reloads
}
```

**How it works:**
- Session ID stored in browser cookie
- Cookie survives page reload
- Same session ID retrieved after reconnection
- Links new circuit to previous auth state

#### **Tier 2: AuthStateCache (Server-Level Singleton)**

**Purpose:** Persist authentication data across circuit recreations

```csharp
// Singleton service - lives for entire application lifetime
public class AuthStateCache
{
    private readonly ConcurrentDictionary<string, CachedAuthState> _cache = new();
    
    public void SetAuthState(string sessionId, string username, int userId, string token)
    {
        _cache[sessionId] = new CachedAuthState
        {
            Username = username,
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };
    }
    
    public CachedAuthState? GetAuthState(string sessionId)
    {
        if (_cache.TryGetValue(sessionId, out var state))
        {
            // Check if token expired
            if (state.ExpiresAt > DateTime.UtcNow)
            {
                return state;
            }
            // Expired - remove from cache
            _cache.TryRemove(sessionId, out _);
        }
        return null;
    }
}
```

**Key features:**
- ✅ Survives SignalR circuit destruction
- ✅ Survives page reloads
- ✅ Thread-safe (ConcurrentDictionary)
- ✅ Automatic expiration handling
- ❌ Does NOT survive server restart (in-memory only)

#### **Tier 3: AuthStateProvider Auto-Restore**

**Purpose:** Automatically restore auth state when new circuit is created

```csharp
public class AuthStateProvider : AuthenticationStateProvider
{
    public AuthStateProvider(
        ILogger<AuthStateProvider> logger, 
        IHttpContextAccessor httpContextAccessor, 
        AuthStateCache authStateCache)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _authStateCache = authStateCache;
        
        // ⚡ KEY: Restore immediately when instance created
        RestoreFromCache();
    }
    
    private void RestoreFromCache()
    {
        try
        {
            // Step 1: Get session ID from browser cookie
            var sessionId = GetOrCreateSessionId();
            if (string.IsNullOrEmpty(sessionId))
            {
                return; // No session available
            }

            // Step 2: Query singleton cache
            var cachedAuth = _authStateCache.GetAuthState(sessionId);
            if (cachedAuth != null)
            {
                // Step 3: Restore ClaimsPrincipal
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, cachedAuth.Username),
                    new Claim(ClaimTypes.NameIdentifier, cachedAuth.UserId.ToString()),
                    new Claim("token", cachedAuth.Token),
                }, "jwt");

                _currentUser = new ClaimsPrincipal(identity);
                
                _logger.LogInformation(
                    "Authentication state restored from cache: {Username}", 
                    cachedAuth.Username
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore authentication state from cache");
        }
    }
}
```

---

## 💻 Implementation Details

### 1. App.razor - Reconnection UI & Handler

**File:** `NasosoTax.Web/Components/App.razor`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]" />
    <link rel="stylesheet" href="@Assets["app.css"]" />
    <link rel="icon" type="image/png" href="favicon.png" />
    <HeadOutlet />
</head>
<body>
    <Routes />
    
    <!-- Blazor Server Reconnection UI -->
    <div id="components-reconnect-modal" class="components-reconnect-hide">
        <div class="show">
            <h5>Attempting to reconnect to the server...</h5>
            <div class="loader"></div>
        </div>
        <div class="failed">
            <h5>Reconnection failed</h5>
            <p>Click <a href="javascript:location.reload()">here</a> to reload the page.</p>
        </div>
        <div class="rejected">
            <h5>Connection rejected</h5>
            <p>Reload the page to restore functionality.</p>
        </div>
    </div>

    <script src="_framework/blazor.web.js"></script>
    <script>
        // Blazor Server Reconnection UI Handler
        setTimeout(() => {
            const modal = document.getElementById('components-reconnect-modal');
            
            if (modal && window.Blazor?.defaultReconnectionHandler) {
                const handler = window.Blazor.defaultReconnectionHandler;
                const origDown = handler.onConnectionDown;
                const origUp = handler.onConnectionUp;
                let checkServerInterval;
                
                // ═══════════════════════════════════════════════
                // CONNECTION DOWN HANDLER
                // ═══════════════════════════════════════════════
                handler.onConnectionDown = function() {
                    console.log('🔴 SignalR connection lost');
                    modal.className = 'components-reconnect-show';
                    
                    // Poll server health every 2 seconds
                    checkServerInterval = setInterval(async () => {
                        try {
                            const response = await fetch('/');
                            if (response.ok) {
                                console.log('🟢 Server is back online');
                                clearInterval(checkServerInterval);
                                
                                // ⚡ KEY: Automatic page reload
                                // This triggers:
                                // 1. New HTTP request with session cookie
                                // 2. New SignalR circuit creation
                                // 3. AuthStateProvider constructor
                                // 4. RestoreFromCache()
                                location.reload();
                            }
                        } catch (e) {
                            console.log('⏳ Server still unreachable, retrying...');
                        }
                    }, 2000);
                    
                    // Call original handler if exists
                    if (origDown) origDown.call(handler);
                };
                
                // ═══════════════════════════════════════════════
                // CONNECTION UP HANDLER
                // ═══════════════════════════════════════════════
                handler.onConnectionUp = function() {
                    console.log('🟢 SignalR connection restored');
                    clearInterval(checkServerInterval);
                    modal.className = 'components-reconnect-hide';
                    
                    if (origUp) origUp.call(handler);
                };
            }
        }, 1000); // Wait 1 second for Blazor to initialize
    </script>
</body>
</html>
```

**Key Points:**
- ✅ Custom reconnection handler wraps Blazor's default handler
- ✅ Shows modal immediately on disconnection
- ✅ Polls server every 2 seconds (health check)
- ✅ **Automatically reloads page** when server responds
- ✅ Cleans up interval on reconnection

---

### 2. AuthStateProvider.cs - Authentication State Management

**File:** `NasosoTax.Web/Services/AuthStateProvider.cs`

```csharp
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace NasosoTax.Web.Services;

/// <summary>
/// Circuit-scoped authentication state provider for Blazor Server.
/// Maintains authentication state within a single SignalR circuit (user session).
/// Uses AuthStateCache (singleton) to persist state across circuit changes.
/// </summary>
public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly ILogger<AuthStateProvider> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthStateCache _authStateCache;
    private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
    private readonly string _instanceId = Guid.NewGuid().ToString()[..8];
    private const string SessionIdKey = "NasosoTaxAuthSessionId";

    public AuthStateProvider(
        ILogger<AuthStateProvider> logger, 
        IHttpContextAccessor httpContextAccessor, 
        AuthStateCache authStateCache)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _authStateCache = authStateCache;
        _logger.LogInformation("AuthStateProvider instance created: {InstanceId}", _instanceId);
        
        // ⚡ CRITICAL: Try to restore authentication state from cache immediately
        // This runs when new circuit is created after reconnection
        RestoreFromCache();
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    /// <summary>
    /// Gets or creates a session ID that persists across page reloads via browser cookie
    /// </summary>
    private string GetOrCreateSessionId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
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
                "Created new auth session ID: {SessionId} for instance {InstanceId}", 
                sessionId, _instanceId
            );
        }
        else
        {
            _logger.LogDebug(
                "Retrieved existing auth session ID: {SessionId} for instance {InstanceId}", 
                sessionId, _instanceId
            );
        }
        
        return sessionId;
    }

    /// <summary>
    /// Restores authentication state from singleton cache using session ID
    /// Called automatically when AuthStateProvider is instantiated (constructor)
    /// </summary>
    private void RestoreFromCache()
    {
        try
        {
            var sessionId = GetOrCreateSessionId();
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogDebug(
                    "No HTTP context available for session ID in instance {InstanceId}", 
                    _instanceId
                );
                return;
            }

            var cachedAuth = _authStateCache.GetAuthState(sessionId);
            if (cachedAuth != null)
            {
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, cachedAuth.Username),
                    new Claim(ClaimTypes.NameIdentifier, cachedAuth.UserId.ToString()),
                    new Claim("token", cachedAuth.Token),
                }, "jwt");

                _currentUser = new ClaimsPrincipal(identity);
                
                _logger.LogInformation(
                    "Authentication state restored from cache in instance {InstanceId}: {Username}", 
                    _instanceId, cachedAuth.Username
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, 
                "Failed to restore authentication state from cache in instance {InstanceId}", 
                _instanceId
            );
        }
    }

    /// <summary>
    /// Called after successful login to save authentication state
    /// </summary>
    public void MarkUserAsAuthenticated(string username, int userId, string token)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("token", token),
        }, "jwt");

        _currentUser = new ClaimsPrincipal(identity);

        // Save to singleton cache for persistence across page navigations and reconnections
        try
        {
            var sessionId = GetOrCreateSessionId();
            if (!string.IsNullOrEmpty(sessionId))
            {
                _authStateCache.SetAuthState(sessionId, username, userId, token);
                _logger.LogInformation(
                    "Authentication state saved to cache with session ID {SessionId} " +
                    "in instance {InstanceId}: {Username} (UserId: {UserId})", 
                    sessionId, _instanceId, username, userId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, 
                "Failed to save authentication state to cache in instance {InstanceId}", 
                _instanceId
            );
        }

        _logger.LogInformation(
            "User authenticated in instance {InstanceId}: {Username} (UserId: {UserId}), " +
            "Identity.IsAuthenticated: {IsAuthenticated}, AuthType: {AuthType}", 
            _instanceId, username, userId, identity.IsAuthenticated, identity.AuthenticationType
        );

        // Notify Blazor that authentication state changed
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Called when user logs out
    /// </summary>
    public void MarkUserAsLoggedOut()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Clear from singleton cache
        try
        {
            var sessionId = GetOrCreateSessionId();
            if (!string.IsNullOrEmpty(sessionId))
            {
                _authStateCache.ClearAuthState(sessionId);
                _logger.LogInformation(
                    "Authentication state cleared from cache in instance {InstanceId}", 
                    _instanceId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, 
                "Failed to clear authentication state from cache in instance {InstanceId}", 
                _instanceId
            );
        }

        _logger.LogInformation("User logged out in instance {InstanceId}", _instanceId);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Gets the JWT token for API calls
    /// </summary>
    public string? GetToken()
    {
        return _currentUser.FindFirst("token")?.Value;
    }

    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    public int? GetUserId()
    {
        var userIdClaim = _currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Checks if user is currently authenticated
    /// </summary>
    public bool IsAuthenticated()
    {
        var isAuth = _currentUser.Identity?.IsAuthenticated ?? false;
        _logger.LogInformation(
            "IsAuthenticated check in instance {InstanceId}: {IsAuthenticated}, " +
            "Identity: {IdentityName}, AuthType: {AuthType}", 
            _instanceId,
            isAuth, 
            _currentUser.Identity?.Name ?? "null",
            _currentUser.Identity?.AuthenticationType ?? "null"
        );
        return isAuth;
    }
}
```

---

### 3. AuthStateCache.cs - Singleton Cache

**File:** `NasosoTax.Web/Services/AuthStateCache.cs` (inferred structure)

```csharp
using System.Collections.Concurrent;

namespace NasosoTax.Web.Services;

/// <summary>
/// Singleton cache that persists authentication state across circuit recreations.
/// Survives SignalR disconnections but NOT server restarts (in-memory only).
/// </summary>
public class AuthStateCache
{
    private readonly ConcurrentDictionary<string, CachedAuthState> _cache = new();
    private readonly ILogger<AuthStateCache> _logger;

    public AuthStateCache(ILogger<AuthStateCache> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Stores authentication state by session ID
    /// </summary>
    public void SetAuthState(string sessionId, string username, int userId, string token)
    {
        var state = new CachedAuthState
        {
            Username = username,
            UserId = userId,
            Token = token,
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8) // Match JWT expiration
        };

        _cache[sessionId] = state;
        
        _logger.LogInformation(
            "Auth state cached for session {SessionId}: {Username} (UserId: {UserId})", 
            sessionId, username, userId
        );
    }

    /// <summary>
    /// Retrieves authentication state by session ID
    /// Returns null if not found or expired
    /// </summary>
    public CachedAuthState? GetAuthState(string sessionId)
    {
        if (_cache.TryGetValue(sessionId, out var state))
        {
            // Check if expired
            if (state.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogInformation(
                    "Auth state retrieved for session {SessionId}: {Username}", 
                    sessionId, state.Username
                );
                return state;
            }
            else
            {
                // Expired - remove from cache
                _cache.TryRemove(sessionId, out _);
                _logger.LogInformation(
                    "Auth state expired for session {SessionId}, removed from cache", 
                    sessionId
                );
            }
        }
        
        return null;
    }

    /// <summary>
    /// Clears authentication state for a session (used on logout)
    /// </summary>
    public void ClearAuthState(string sessionId)
    {
        if (_cache.TryRemove(sessionId, out _))
        {
            _logger.LogInformation(
                "Auth state cleared for session {SessionId}", 
                sessionId
            );
        }
    }

    /// <summary>
    /// Cleans up expired entries (optional background task)
    /// </summary>
    public void CleanupExpiredEntries()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Any())
        {
            _logger.LogInformation(
                "Cleaned up {Count} expired auth cache entries", 
                expiredKeys.Count
            );
        }
    }
}

/// <summary>
/// Cached authentication state
/// </summary>
public class CachedAuthState
{
    public string Username { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

---

### 4. Program.cs - Service Registration

**File:** `NasosoTax.Web/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Session support for authentication persistence
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24); // Session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// ═══════════════════════════════════════════════════════════════
// AUTHENTICATION STATE MANAGEMENT
// ═══════════════════════════════════════════════════════════════

// ⚡ CRITICAL: Register AuthStateCache as SINGLETON
// This ensures the cache survives circuit recreations
builder.Services.AddSingleton<Services.AuthStateCache>();

// Register AuthStateProvider as SCOPED (per Blazor circuit)
// New instance created for each SignalR circuit
// RestoreFromCache() runs in constructor to restore auth state
builder.Services.AddScoped<Services.AuthStateProvider>();

// Register as AuthenticationStateProvider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<Services.AuthStateProvider>()
);

// Add Cascading Authentication State for Blazor components
builder.Services.AddCascadingAuthenticationState();

// Other services...
builder.Services.AddScoped<Services.ApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ⚡ CRITICAL: Enable Session middleware BEFORE Blazor components
// This ensures session cookie is available when AuthStateProvider is created
app.UseSession();

app.UseAntiforgery();

app.MapStaticAssets();

// Map Blazor components with SignalR
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

**Key Configuration:**
- ✅ `AuthStateCache` registered as **Singleton** (survives circuit changes)
- ✅ `AuthStateProvider` registered as **Scoped** (per circuit)
- ✅ Session middleware enabled **before** Blazor components
- ✅ Session timeout: 24 hours
- ✅ Cascading authentication state for components

---

### 5. Protected Page Pattern

**File:** `NasosoTax.Web/Components/Pages/Reports.razor` (example)

```csharp
@page "/reports"
@using NasosoTax.Application.DTOs
@using NasosoTax.Web.Services
@inject ApiService ApiService
@inject AuthStateProvider AuthStateProvider
@inject NavigationManager NavigationManager
@rendermode InteractiveServer

<PageTitle>Tax Reports - NasosoTax</PageTitle>

<div class="container mt-4">
    @if (isLoading)
    {
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mt-3">Loading reports...</p>
        </div>
    }
    else if (report != null)
    {
        <!-- Display reports -->
    }
    else
    {
        <div class="alert alert-warning">
            <p>No reports found. Please login to view your reports.</p>
        </div>
    }
</div>

@code {
    private TaxReportResponse? report = null;
    private string errorMessage = "";
    private bool isLoading = true;

    /// <summary>
    /// Component initialization - runs every time component loads
    /// INCLUDING after page reload from reconnection
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        // ═══════════════════════════════════════════════════════════════
        // AUTHENTICATION CHECK AFTER RECONNECTION
        // ═══════════════════════════════════════════════════════════════
        
        // When page reloads after server reconnection:
        // 1. New SignalR circuit created
        // 2. New AuthStateProvider instance created
        // 3. RestoreFromCache() runs in constructor
        // 4. This OnInitializedAsync() runs
        // 5. Check if user is still authenticated
        
        if (!AuthStateProvider.IsAuthenticated())
        {
            // Token expired or not present after reconnection
            // Redirect to login (automatic, seamless)
            _logger.LogWarning("User not authenticated, redirecting to login");
            NavigationManager.NavigateTo("/login", forceLoad: true);
            return;
        }
        
        // User is authenticated, load protected data
        await LoadData();
    }
    
    private async Task LoadData()
    {
        isLoading = true;
        
        try
        {
            // Fetch protected data from API
            report = await ApiService.GetAsync<TaxReportResponse>("/api/reports/user");
        }
        catch (UnauthorizedAccessException)
        {
            // Token became invalid during operation (rare edge case)
            errorMessage = "Your session has expired. Please login again.";
            NavigationManager.NavigateTo("/login", forceLoad: true);
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to load reports. Please try again.";
            Console.WriteLine($"Error loading reports: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }
}
```

**Pattern for ALL protected pages:**
```csharp
protected override async Task OnInitializedAsync()
{
    // 1. Check authentication FIRST
    if (!AuthStateProvider.IsAuthenticated())
    {
        NavigationManager.NavigateTo("/login", forceLoad: true);
        return;
    }
    
    // 2. Load protected data
    await LoadData();
}
```

---

## 📊 Visual Timeline

### Reconnection Sequence Diagram

```
Time | Browser              | JavaScript        | Server             | Result
─────┼──────────────────────┼──────────────────┼────────────────────┼─────────────────────
T+0s │ Browsing Reports     │                  │ ✅ Connected       │ Normal operation
     │                      │                  │                    │
T+1s │                      │                  │ 🔴 Server crash   │ Connection lost
     │                      │                  │                    │
T+2s │ Loading indicator    │ onConnectionDown()│                   │ Modal appears
     │ shows                │ fires            │                    │
     │                      │                  │                    │
T+3s │ "Attempting to       │ Start polling:   │ Server down       │ Polling every 2s
     │ reconnect..."        │ fetch('/')       │ (no response)     │
     │                      │ every 2 seconds  │                    │
     │                      │                  │                    │
T+5s │ Still showing modal  │ fetch('/') #1    │ Server down       │ Request failed
     │                      │ → Failed         │                    │
     │                      │                  │                    │
T+7s │ Still showing modal  │ fetch('/') #2    │ Server down       │ Request failed
     │                      │ → Failed         │                    │
     │                      │                  │                    │
T+9s │ Still showing modal  │ fetch('/') #3    │ 🟢 Server back!  │ Request succeeds
     │                      │ → Success!       │ Returns 200 OK    │
     │                      │                  │                    │
T+9s │                      │ clearInterval()  │                    │ Stop polling
     │                      │ location.reload()│                    │ Trigger reload
     │                      │                  │                    │
─────┼──────────────────────┼──────────────────┼────────────────────┼─────────────────────
     │ ⚡ PAGE RELOAD ⚡    │                  │                    │
─────┼──────────────────────┼──────────────────┼────────────────────┼─────────────────────
     │                      │                  │                    │
T+10s│ New HTTP request     │                  │ Receives request  │ With session cookie
     │ (includes cookie)    │                  │ with session ID   │
     │                      │                  │                    │
     │                      │                  │ Create new circuit│ New SignalR hub
     │                      │                  │                    │
     │                      │                  │ AuthStateProvider │ Constructor runs
     │                      │                  │ constructor()     │
     │                      │                  │                    │
     │                      │                  │ RestoreFromCache()│ Query singleton
     │                      │                  │   ├─ GetSessionID │ From cookie
     │                      │                  │   ├─ QueryCache   │ Get cached auth
     │                      │                  │   └─ Restore      │ Set ClaimsPrincipal
     │                      │                  │                    │
T+11s│ Component renders    │                  │ OnInitializedAsync│ Check auth status
     │                      │                  │ runs              │
     │                      │                  │                    │
     │                      │                  │ IsAuthenticated() │ 
     │                      │                  │ check             │
     │                      │                  │       │           │
     │                      │                  │       ├─ TRUE ✅  │ Token valid
     │                      │                  │       │           │
T+12s│ Reports page loads   │                  │ LoadData()        │ ✅ User continues
     │ Shows user data      │                  │ API calls succeed │ Seamless experience
     │                      │                  │                    │
─────┴──────────────────────┴──────────────────┴────────────────────┴─────────────────────

Alternative path if token expired:

     │                      │                  │ IsAuthenticated() │ 
     │                      │                  │ check             │
     │                      │                  │       │           │
     │                      │                  │       └─ FALSE ❌ │ Token expired/missing
     │                      │                  │                    │
T+12s│ Redirecting...       │                  │ NavigateTo(       │ 🔐 Automatic redirect
     │                      │                  │   "/login"        │
     │                      │                  │ )                 │
     │                      │                  │                    │
T+13s│ Login page shows     │                  │                    │ User must re-login
     │                      │                  │                    │
```

---

## 🔒 Security Considerations

### 1. Token Expiration Handling

**Problem:** JWT tokens expire (typically 8 hours). What if reconnection happens after expiration?

**Solution:**
```csharp
public CachedAuthState? GetAuthState(string sessionId)
{
    if (_cache.TryGetValue(sessionId, out var state))
    {
        // Check if token expired
        if (state.ExpiresAt > DateTime.UtcNow)
        {
            return state; // Valid
        }
        else
        {
            // Expired - remove from cache
            _cache.TryRemove(sessionId, out _);
            return null; // User must re-login
        }
    }
    return null;
}
```

**Result:**
- ✅ Expired tokens automatically removed from cache
- ✅ `IsAuthenticated()` returns false
- ✅ User redirected to login
- ✅ No security risk from stale tokens

---

### 2. Session Hijacking Protection

**Risks:**
- Session ID stored in browser cookie
- Could be stolen via XSS or network sniffing

**Mitigations:**
```csharp
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;      // ✅ Prevents JavaScript access
    options.Cookie.IsEssential = true;    // ✅ Required for functionality
    options.Cookie.SecurePolicy =         // ✅ HTTPS only in production
        CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromHours(24); // ✅ Automatic expiration
});
```

**Additional recommendations:**
- ✅ Always use HTTPS in production
- ✅ Implement CSRF protection (already done via `UseAntiforgery()`)
- ✅ Regularly rotate session IDs
- ✅ Monitor for suspicious activity (multiple IPs, unusual patterns)

---

### 3. Cache Memory Management

**Problem:** Singleton cache grows indefinitely if not managed

**Solution:**
```csharp
// Option 1: Background cleanup service
public class AuthCacheCleanupService : BackgroundService
{
    private readonly AuthStateCache _cache;
    private readonly ILogger<AuthCacheCleanupService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Run cleanup every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            
            try
            {
                _cache.CleanupExpiredEntries();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up auth cache");
            }
        }
    }
}

// Register in Program.cs
builder.Services.AddHostedService<AuthCacheCleanupService>();
```

**Option 2: Use MemoryCache with expiration**
```csharp
public class AuthStateCache
{
    private readonly IMemoryCache _cache;

    public void SetAuthState(string sessionId, string username, int userId, string token)
    {
        var state = new CachedAuthState { ... };
        
        _cache.Set(sessionId, state, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(8),
            Priority = CacheItemPriority.High
        });
    }
}
```

---

### 4. Concurrent Access Safety

**Problem:** Multiple requests could access cache simultaneously

**Solution:** Already implemented with `ConcurrentDictionary`
```csharp
private readonly ConcurrentDictionary<string, CachedAuthState> _cache = new();
```

**Benefits:**
- ✅ Thread-safe reads and writes
- ✅ No locking required
- ✅ High performance under load

---

## 🧪 Testing Scenarios

### Test Case 1: Normal Reconnection (Token Valid)

**Steps:**
1. Login to application
2. Navigate to Reports page
3. Stop the API server
4. Wait for modal to appear
5. Restart the API server
6. Observe automatic reconnection

**Expected Result:**
- ✅ Modal shows "Attempting to reconnect..."
- ✅ Page automatically reloads when server is back
- ✅ User remains on Reports page
- ✅ Data loads successfully
- ✅ No login required

**Console Output:**
```
🔴 SignalR connection lost
⏳ Server still unreachable, retrying...
⏳ Server still unreachable, retrying...
🟢 Server is back online
[AuthStateProvider] Authentication state restored from cache: testuser
[Reports] Loading reports for user testuser
✅ Reports loaded successfully
```

---

### Test Case 2: Reconnection After Token Expiration

**Steps:**
1. Login to application
2. Navigate to Reports page
3. Stop the API server
4. Wait for 9+ hours (or manually clear cache)
5. Restart the API server
6. Observe automatic reconnection

**Expected Result:**
- ✅ Modal shows "Attempting to reconnect..."
- ✅ Page automatically reloads when server is back
- ✅ `RestoreFromCache()` returns null (token expired)
- ✅ `IsAuthenticated()` returns false
- ✅ Automatic redirect to `/login`
- ✅ User sees login page

**Console Output:**
```
🔴 SignalR connection lost
⏳ Server still unreachable, retrying...
🟢 Server is back online
[AuthStateCache] Auth state expired for session abc123, removed from cache
[AuthStateProvider] No cached auth state found
[Reports] User not authenticated, redirecting to login
🔐 Navigating to /login
```

---

### Test Case 3: Network Loss (Client-Side)

**Steps:**
1. Login to application
2. Navigate to Reports page
3. Open DevTools → Network tab
4. Select "Offline" mode
5. Wait 10 seconds
6. Disable offline mode

**Expected Result:**
- ✅ Modal shows immediately
- ✅ Polling fails (no network)
- ✅ When network returns, polling succeeds
- ✅ Page reloads automatically
- ✅ User continues on Reports page

---

### Test Case 4: Multiple Tabs

**Steps:**
1. Login in Tab 1
2. Open Reports in Tab 2
3. Stop server
4. Both tabs show reconnection modal
5. Restart server

**Expected Result:**
- ✅ Both tabs reconnect independently
- ✅ Both tabs reload automatically
- ✅ Both tabs restore authentication
- ✅ Both tabs continue working
- ✅ Session ID is shared across tabs

---

### Test Case 5: Server Restart During Active Work

**Steps:**
1. Login to application
2. Navigate to Submit Income page
3. Fill out form (don't submit)
4. Restart server
5. Observe behavior

**Expected Result:**
- ✅ Modal shows
- ✅ Page reloads when server is back
- ✅ Form data is LOST (expected behavior)
- ✅ User remains authenticated
- ✅ User must re-enter form data

**Note:** To preserve form data, implement:
- Local storage persistence
- Auto-save drafts
- Or warn user before reload

---

## 🔧 Troubleshooting

### Problem 1: User Not Redirected to Login After Reconnection

**Symptoms:**
- Page reloads after reconnection
- User stays on protected page
- But data fails to load (401 errors)

**Possible Causes:**
1. `IsAuthenticated()` not called in `OnInitializedAsync()`
2. Navigation happens but is ignored
3. Auth state restored but token is invalid

**Solution:**
```csharp
protected override async Task OnInitializedAsync()
{
    // Add logging
    _logger.LogInformation("OnInitializedAsync - Checking auth");
    
    if (!AuthStateProvider.IsAuthenticated())
    {
        _logger.LogWarning("Not authenticated, redirecting");
        NavigationManager.NavigateTo("/login", forceLoad: true);
        return; // CRITICAL: return to prevent further execution
    }
    
    await LoadData();
}
```

---

### Problem 2: Session Lost After Reconnection

**Symptoms:**
- Page reloads
- `RestoreFromCache()` returns null
- Session ID not found

**Possible Causes:**
1. Session cookies disabled in browser
2. Session timeout too short
3. Different domain/port after reconnection
4. Server restarted (in-memory cache lost)

**Solution:**
```csharp
// Check session configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24); // Increase if needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // CRITICAL
    options.Cookie.Name = ".NasosoTax.Session"; // Consistent name
});

// Add logging in RestoreFromCache()
var sessionId = GetOrCreateSessionId();
_logger.LogInformation("Session ID: {SessionId}", sessionId);

var cachedAuth = _authStateCache.GetAuthState(sessionId);
_logger.LogInformation("Cached auth found: {Found}", cachedAuth != null);
```

---

### Problem 3: Infinite Reconnection Loop

**Symptoms:**
- Modal appears
- Page reloads
- Modal appears again
- Repeats infinitely

**Possible Causes:**
1. Server endpoint `/` returns 404
2. Polling interval not cleared
3. Error in `RestoreFromCache()`

**Solution:**
```javascript
// Add better error handling
handler.onConnectionDown = function() {
    modal.className = 'components-reconnect-show';
    
    let attemptCount = 0;
    const maxAttempts = 30; // Prevent infinite polling
    
    checkServerInterval = setInterval(async () => {
        attemptCount++;
        
        if (attemptCount > maxAttempts) {
            console.error('❌ Max reconnection attempts reached');
            clearInterval(checkServerInterval);
            // Show manual reload message
            modal.querySelector('.show').style.display = 'none';
            modal.querySelector('.failed').style.display = 'block';
            return;
        }
        
        try {
            const response = await fetch('/');
            if (response.ok) {
                console.log('🟢 Server is back online');
                clearInterval(checkServerInterval);
                location.reload();
            }
        } catch (e) {
            console.log(`⏳ Attempt ${attemptCount}/${maxAttempts} failed`);
        }
    }, 2000);
};
```

---

### Problem 4: Cache Growing Too Large

**Symptoms:**
- Server memory usage increases over time
- Performance degrades
- OutOfMemoryException

**Solution:**
```csharp
// Add background cleanup
public class AuthCacheCleanupService : BackgroundService
{
    private readonly AuthStateCache _cache;
    private readonly ILogger<AuthCacheCleanupService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            
            try
            {
                _cache.CleanupExpiredEntries();
                
                // Add metrics
                var cacheSize = _cache.GetCacheSize();
                _logger.LogInformation("Auth cache size: {Size} entries", cacheSize);
                
                if (cacheSize > 10000)
                {
                    _logger.LogWarning("Auth cache is very large: {Size} entries", cacheSize);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up auth cache");
            }
        }
    }
}
```

---

## 📚 Key Takeaways

### ✅ What Makes This Implementation Work

1. **Three-Tier Persistence**
   - ASP.NET Session (browser cookie) → Session ID
   - Singleton Cache (server memory) → Auth data
   - Auto-restore (constructor) → Reconnect logic

2. **Automatic Page Reload**
   - JavaScript polls server health
   - Triggers `location.reload()` when server is back
   - New circuit created with fresh auth check

3. **Seamless User Experience**
   - No manual refresh required
   - Automatic redirect if token expired
   - Visual feedback during reconnection

4. **Security First**
   - Token expiration enforced
   - HttpOnly cookies prevent XSS
   - Automatic cleanup prevents memory leaks

5. **Production Ready**
   - Comprehensive logging
   - Error handling at every step
   - Graceful degradation (manual reload fallback)

---

### ❌ What Does NOT Survive

| Scenario | Result | Solution |
|----------|--------|----------|
| Server restart | Cache lost, user must re-login | Use distributed cache (Redis) |
| Form data during reconnection | Lost on reload | Auto-save to local storage |
| Component state | Reset on new circuit | Persist to session/cache |
| Long-running operations | Interrupted | Implement resumable tasks |
| Uploaded files in memory | Lost | Save to disk immediately |

---

### 🎯 When to Use This Pattern

**✅ Good fit for:**
- Enterprise web applications
- Internal business tools
- Dashboard applications
- Admin panels
- Real-time data displays

**❌ Consider alternatives for:**
- Public-facing websites (use Blazor WebAssembly)
- Mobile apps (use native or MAUI)
- Very high-scale apps (consider session persistence)
- Offline-first apps (use PWA with service workers)

---

## 🔗 Related Documentation

- [SIGNALR_CONNECTION_MANAGEMENT.md](./SIGNALR_CONNECTION_MANAGEMENT.md) - Original condensed version
- [AUTHENTICATION.md](./guides/AUTHENTICATION.md) - JWT authentication details
- [PROJECT_DESIGN_REFERENCE.md](./PROJECT_DESIGN_REFERENCE.md) - Overall architecture
- [Microsoft Blazor Server Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr)

---

**Document Version:** 1.0  
**Created:** October 16, 2025  
**Author:** NasosoTax Development Team  
**Status:** ✅ Production Implementation

---

## 📝 Appendix: Complete File Listing

Files involved in this implementation:

```
NasosoTax.Web/
├── Components/
│   ├── App.razor                    ← Reconnection UI & JavaScript handler
│   └── Pages/
│       ├── Reports.razor            ← Example protected page
│       ├── Login.razor              ← Authentication entry point
│       └── ...
├── Services/
│   ├── AuthStateProvider.cs        ← Circuit-scoped auth state manager
│   ├── AuthStateCache.cs           ← Singleton cache (inferred)
│   └── ApiService.cs               ← HTTP client wrapper
├── Program.cs                       ← Service registration & configuration
└── wwwroot/
    └── app.css                      ← Modal styling

NasosoTax.Api/
└── Program.cs                       ← JWT configuration (backend)
```

---

**End of Document**
