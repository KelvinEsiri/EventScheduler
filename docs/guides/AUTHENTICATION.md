# Authentication System

## Overview

NasosoTax uses a JWT-based authentication system designed for Blazor Server applications. The authentication state is maintained per SignalR circuit (user session) and automatically protects access to General Ledger and Tax Report Management features.

## Architecture

### Components

1. **AuthStateProvider** (`Services/AuthStateProvider.cs`)
   - Circuit-scoped authentication state provider
   - Maintains user claims (username, userId, JWT token)
   - Implements ASP.NET Core's `AuthenticationStateProvider`
   - Notifies components when authentication state changes

2. **ApiService** (`Services/ApiService.cs`)
   - Handles all HTTP requests to the API
   - Automatically includes JWT token in request headers
   - Throws `UnauthorizedAccessException` on 401 responses
   - Supports GET, POST, PUT, DELETE operations

3. **Protected Pages**
   - Login.razor (public)
   - Register.razor (public)
   - Calculator.razor (public)
   - **Ledger.razor (protected)** - General Ledger access
   - **Reports.razor (protected)** - Tax Reports access
   - **SubmitIncome.razor (protected)** - Tax submission
   - Logout.razor (public)

## Authentication Flow

### 1. Login Process

```
User → Login.razor → /api/auth/login → API returns JWT token
                   ↓
         AuthStateProvider.MarkUserAsAuthenticated(username, userId, token)
                   ↓
         ApiService.SetToken(token) → Sets Authorization header
                   ↓
         Navigate to /calculator
```

### 2. Protected Page Access

```
User navigates to /ledger
         ↓
OnInitializedAsync() runs
         ↓
AuthStateProvider.IsAuthenticated() check
         ↓
   ┌─────┴─────┐
   ↓           ↓
 TRUE        FALSE
   ↓           ↓
Load data   Redirect to /login
```

### 3. API Request Flow

```
Page calls ApiService.GetAsync("/api/ledger")
         ↓
EnsureToken() → Gets token from AuthStateProvider
         ↓
Sets Authorization: Bearer <token> header
         ↓
Makes HTTP request
         ↓
   ┌─────┴─────┐
   ↓           ↓
 200 OK     401 Unauthorized
   ↓           ↓
Return data   Throw UnauthorizedAccessException
                     ↓
              Page catches exception → Redirect to /login
```

### 4. Logout Process

```
User clicks Logout → Navigate to /logout
         ↓
AuthStateProvider.MarkUserAsLoggedOut()
         ↓
ApiService.ClearToken()
         ↓
Navigate to /login with forceLoad: true
```

## Implementation Details

### Setting Up Authentication in a Page

```csharp
@page "/mypage"
@inject AuthStateProvider AuthStateProvider
@inject ApiService ApiService
@inject NavigationManager NavigationManager
@rendermode InteractiveServer

@code {
    protected override async Task OnInitializedAsync()
    {
        // Check authentication before loading
        if (!AuthStateProvider.IsAuthenticated())
        {
            NavigationManager.NavigateTo("/login", forceLoad: true);
            return;
        }
        
        await LoadData();
    }

    private async Task LoadData()
    {
        try
        {
            var data = await ApiService.GetAsync<MyData>("/api/myendpoint");
            // Process data
        }
        catch (UnauthorizedAccessException)
        {
            // Session expired
            NavigationManager.NavigateTo("/login", forceLoad: true);
        }
    }
}
```

### JWT Token Structure

The JWT token contains the following claims:
- `ClaimTypes.Name` - Username
- `ClaimTypes.NameIdentifier` - User ID
- Standard JWT claims (issuer, audience, expiration)

### API Controllers

All protected API controllers use the `[Authorize]` attribute:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LedgerController : ControllerBase
{
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}
```

## Security Features

1. **JWT Token Validation**
   - Validates issuer, audience, lifetime, and signing key
   - Configured in `Program.cs`

2. **Circuit-Scoped State**
   - Authentication state is isolated per user session
   - No cross-session data leakage

3. **Automatic Token Inclusion**
   - All API requests automatically include the JWT token
   - No manual token management needed in pages

4. **Consistent Error Handling**
   - 401 responses throw `UnauthorizedAccessException`
   - Pages can catch and redirect to login

## Configuration

### JWT Settings (appsettings.json)

```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyForNasosoTaxPortal2025MinimumLength32Characters!",
    "Issuer": "NasosoTax",
    "Audience": "NasosoTaxUsers"
  }
}
```

### Service Registration (Program.cs)

```csharp
// Authentication State Provider
builder.Services.AddScoped<Services.AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<Services.AuthStateProvider>());

// API Service
builder.Services.AddScoped<Services.ApiService>();

// Cascading Authentication State
builder.Services.AddCascadingAuthenticationState();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* ... */ });
```

## Testing

### Test Authentication Flow

1. **Verify Login**
   - Navigate to http://localhost:5070/login
   - Enter valid credentials
   - Should redirect to /calculator
   - Check logs for "User authenticated" message

2. **Verify Protected Page Access**
   - After login, navigate to /ledger
   - Should load without redirect
   - Should display ledger data

3. **Verify Unauthenticated Access**
   - Open new browser window (incognito)
   - Navigate directly to /ledger
   - Should redirect to /login

4. **Verify Session Persistence**
   - Login and navigate to /ledger
   - Click on /reports
   - Click back to /ledger
   - Should maintain authentication throughout

5. **Verify Logout**
   - Click Logout
   - Should redirect to /login
   - Try accessing /ledger
   - Should redirect to /login

## Troubleshooting

### Common Issues

1. **302 Redirects After Login**
   - **Cause**: Token not being set in ApiService
   - **Fix**: Ensure `ApiService.SetToken()` is called after login
   - **Verify**: Check `AuthStateProvider.GetToken()` returns token

2. **Authentication Not Persisting**
   - **Cause**: AuthStateProvider not registered as scoped
   - **Fix**: Verify service registration in Program.cs
   - **Verify**: Check logs for "AuthStateProvider instance created"

3. **401 Errors on API Calls**
   - **Cause**: Token expired or invalid
   - **Fix**: Check token expiration in JWT configuration
   - **Verify**: Decode JWT token and check expiration claim

4. **Cross-Origin Errors**
   - **Cause**: CORS not configured properly
   - **Fix**: Verify CORS policy in Program.cs
   - **Verify**: Check browser console for CORS errors

## Best Practices

1. **Always Check Authentication First**
   - Check `IsAuthenticated()` in `OnInitializedAsync()`
   - Redirect to login before making API calls

2. **Handle Unauthorized Exceptions**
   - Wrap API calls in try-catch blocks
   - Catch `UnauthorizedAccessException` specifically
   - Redirect to login with clear error message

3. **Use forceLoad for Login Redirects**
   - Always use `NavigateTo("/login", forceLoad: true)`
   - Ensures proper circuit reset on authentication failure

4. **Secure Sensitive Operations**
   - Double-check authentication before destructive operations
   - Use `[Authorize]` attribute on all protected API controllers

## Future Enhancements

Potential improvements to consider:

1. **Token Refresh**
   - Implement automatic token refresh before expiration
   - Add refresh token endpoint

2. **Remember Me**
   - Add persistent authentication option
   - Store encrypted token securely

3. **Multi-Factor Authentication**
   - Add TOTP support
   - SMS/Email verification

4. **Session Management**
   - Track active sessions
   - Allow users to view/revoke sessions

5. **Password Reset**
   - Email-based password reset flow
   - Security questions

## Support

For issues or questions:
- Check application logs in `internal/logs/`
- Review Serilog output for authentication events
- Verify JWT token structure and claims
