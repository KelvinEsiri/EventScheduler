# NasosoTax - Complete Project Design Reference

**Project:** Tax Management Portal (Nigeria Tax Act 2025)  
**Version:** 1.1.0  
**Date:** October 2025  
**Status:** ✅ Production Ready  
**Architecture Rating:** ⭐⭐⭐⭐⭐ (5/5)

---

## 📋 Quick Overview

**NasosoTax** is a full-stack enterprise-grade tax management system built with **C# .NET 9.0** implementing **Nigeria Tax Act 2025**. It demonstrates Clean Architecture with complete frontend/backend separation, JWT authentication, progressive tax calculations, and comprehensive financial tracking.

**Key Stats:**
- **5 Layers:** Domain, Application, Infrastructure, API, Web
- **6 Tax Brackets:** Progressive taxation (0% - 25%)
- **5 API Controllers:** Auth, Tax, Ledger, Reports, Health
- **25+ Features:** Tax calculation, income tracking, ledger, reporting
- **2 Ports:** API (5001), Web (5070)

---

## 🏗️ Architecture

### **Clean Architecture (5 Layers)**

```
┌─────────────────────────────────────────────────┐
│              NasosoTax.Web (Port 5070)          │
│         Blazor Server + Bootstrap 5 UI          │
└────────────────┬────────────────────────────────┘
                 │ HTTP/REST
                 ↓
┌─────────────────────────────────────────────────┐
│              NasosoTax.Api (Port 5001)          │
│         Controllers + JWT + Middleware          │
└────────────────┬────────────────────────────────┘
                 │
    ┌────────────┴────────────┐
    ↓                         ↓
┌─────────────────┐   ┌──────────────────┐
│  Application    │   │  Infrastructure  │
│  (Services)     │   │  (Repositories)  │
└────────┬────────┘   └────────┬─────────┘
         │                     │
         └──────────┬──────────┘
                    ↓
         ┌──────────────────┐
         │     Domain       │
         │   (Entities)     │
         └──────────────────┘
```

### **Layer Responsibilities**

| Layer | Purpose | Dependencies |
|-------|---------|--------------|
| **Domain** | Entities & Models | None |
| **Application** | Business Logic & Interfaces | Domain |
| **Infrastructure** | Data Access & EF Core | Domain, Application |
| **API** | RESTful Backend | Application, Infrastructure |
| **Web** | Blazor UI | Application (DTOs only) |

### **Key Patterns Used**

- ✅ **Repository Pattern** - Data access abstraction
- ✅ **Service Pattern** - Business logic encapsulation
- ✅ **DTO Pattern** - API data contracts
- ✅ **Dependency Injection** - Loose coupling
- ✅ **Middleware Pattern** - Cross-cutting concerns
- ✅ **Circuit-Scoped Services** - Blazor Server state

---

## 🔐 Authentication & Authorization

### **Implementation: JWT-Based Authentication**

**Technology Stack:**
- JWT Bearer tokens (8-hour expiration)
- PBKDF2 password hashing (100,000 iterations, SHA-256)
- Circuit-scoped authentication state (Blazor Server)
- Automatic token injection in API requests

---

## 🔄 Complete Authentication Flow

### **1. Initial Login Process**

```
┌─────────────┐
│   User      │
│ (Browser)   │
└──────┬──────┘
       │ 1. Navigate to /login
       ↓
┌─────────────────────────────────────────┐
│         Login.razor (Frontend)          │
│  - Username/Password input fields       │
│  - Client-side validation               │
└──────┬──────────────────────────────────┘
       │ 2. Click "Login" button
       ↓
┌─────────────────────────────────────────┐
│      ApiService.PostAsync()             │
│  - Sends POST /api/auth/login           │
│  - Body: { username, password }         │
└──────┬──────────────────────────────────┘
       │ 3. HTTP Request
       ↓
┌─────────────────────────────────────────┐
│  AuthController.Login() (Backend API)   │
│  - Receives credentials                 │
└──────┬──────────────────────────────────┘
       │ 4. Validate request
       ↓
┌─────────────────────────────────────────┐
│     AuthService.AuthenticateAsync()     │
│  - Find user by username                │
│  - Verify password hash (PBKDF2)        │
└──────┬──────────────────────────────────┘
       │ 5. Validation result
       ├─────────────┬───────────────┐
       ↓             ↓               ↓
   ✅ Valid      ❌ Invalid       ❌ User not found
       │             │               │
       │             └───────┬───────┘
       │                     ↓
       │            Return 401 Unauthorized
       │            { "message": "Invalid credentials" }
       │                     │
       │                     ↓
       │            Login.razor catches error
       │            Shows error message
       │            User stays on /login
       │
       ↓
   Generate JWT Token
   - Claims: username, userId
   - Expiration: 8 hours
   - Signed with secret key
       │
       ↓
   Return 200 OK
   {
     "token": "eyJhbGc...",
     "username": "john",
     "email": "john@example.com",
     "fullName": "John Doe"
   }
       │
       ↓
┌─────────────────────────────────────────┐
│     Login.razor receives response       │
│  1. AuthStateProvider.MarkUserAsAuth()  │
│     - Stores username, userId, token    │
│     - Creates ClaimsPrincipal           │
│     - Notifies auth state changed       │
│  2. ApiService.SetToken(token)          │
│     - Sets Authorization header         │
│  3. NavigationManager.NavigateTo("/")   │
└──────┬──────────────────────────────────┘
       │
       ↓
   User redirected to Home
   NavMenu shows "Welcome, John"
   Protected links become visible
```

---

### **2. Detailed Implementation Components**

#### **A. AuthStateProvider (Circuit-Scoped Service)**

**File:** `NasosoTax.Web/Services/AuthStateProvider.cs`

**Purpose:** Manages authentication state for the current user session (SignalR circuit)

**Key Methods:**

```csharp
public class AuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
    private string? _token;
    private string? _username;
    private int? _userId;

    // Called when user logs in
    public void MarkUserAsAuthenticated(string username, int userId, string token)
    {
        _username = username;
        _userId = userId;
        _token = token;
        
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "jwt");
        
        _currentUser = new ClaimsPrincipal(identity);
        
        // Notify all components that auth state changed
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_currentUser))
        );
    }

    // Called when user logs out or session expires
    public void MarkUserAsLoggedOut()
    {
        _username = null;
        _userId = null;
        _token = null;
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_currentUser))
        );
    }

    // Check if user is authenticated
    public bool IsAuthenticated()
    {
        return _currentUser?.Identity?.IsAuthenticated ?? false;
    }

    // Get stored JWT token
    public string? GetToken() => _token;
    
    // Get current username
    public string? GetUsername() => _username;
    
    // Get current userId
    public int? GetUserId() => _userId;

    // Required by AuthenticationStateProvider
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }
}
```

**Scope:** Circuit-scoped (one instance per user session/browser tab)

**Lifecycle:**
- Created when user connects (SignalR circuit established)
- Persists for the duration of the session
- Destroyed when circuit disconnects (browser closed/navigated away)

---

#### **B. ApiService (HTTP Client Wrapper)**

**File:** `NasosoTax.Web/Services/ApiService.cs`

**Purpose:** Centralized HTTP communication with automatic token injection

**Key Methods:**

```csharp
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthStateProvider _authStateProvider;

    // Set token in HTTP client headers
    public void SetToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }

    // Clear token from headers
    public void ClearToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    // Ensure token is present before making requests
    private void EnsureToken()
    {
        var token = _authStateProvider.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // Generic GET request with automatic auth handling
    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            EnsureToken(); // Inject token into header
            
            var response = await _httpClient.GetAsync(url);
            
            // Handle 401 Unauthorized
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException(
                    "Session expired. Please log in again."
                );
            }
            
            if (!response.IsSuccessStatusCode)
                return default;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, 
                new JsonSerializerOptions { 
                    PropertyNameCaseInsensitive = true 
                });
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw to let calling component handle redirect
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GET request");
            throw;
        }
    }

    // Similar implementation for PostAsync, PutAsync, DeleteAsync
}
```

**Flow:**
1. Component calls `ApiService.GetAsync()`
2. `EnsureToken()` injects JWT into `Authorization: Bearer {token}` header
3. Makes HTTP request to API
4. If 401 response → throws `UnauthorizedAccessException`
5. Component catches exception → redirects to login

---

#### **C. Protected Page Pattern**

**Example:** `Reports.razor`

```csharp
@page "/reports"
@inject AuthStateProvider AuthStateProvider
@inject ApiService ApiService
@inject NavigationManager NavigationManager
@rendermode InteractiveServer

<PageTitle>Tax Reports</PageTitle>

@if (isLoading)
{
    <div class="text-center">
        <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
    </div>
}
else
{
    <!-- Protected content here -->
}

@code {
    private bool isLoading = true;
    private List<YearlySummary>? summaries;

    protected override async Task OnInitializedAsync()
    {
        // 🔒 AUTHENTICATION CHECK - First line of defense
        if (!AuthStateProvider.IsAuthenticated())
        {
            // User not logged in → redirect to login
            NavigationManager.NavigateTo("/login", forceLoad: true);
            return; // Stop execution
        }

        try
        {
            // User is authenticated → load data
            await LoadReports();
        }
        catch (UnauthorizedAccessException ex)
        {
            // 🔒 Token expired or invalid during API call
            Console.WriteLine($"Auth error: {ex.Message}");
            NavigationManager.NavigateTo("/login", forceLoad: true);
        }
        catch (Exception ex)
        {
            // Handle other errors
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadReports()
    {
        // This throws UnauthorizedAccessException if token invalid
        summaries = await ApiService.GetAsync<List<YearlySummary>>(
            "/api/reports/yearly-summaries"
        );
    }
}
```

**Two-Layer Protection:**
1. **Client-side check:** `IsAuthenticated()` - prevents unnecessary API calls
2. **Server-side validation:** API validates JWT - prevents unauthorized access

---

#### **D. API Authentication Setup**

**File:** `NasosoTax.Api/Program.cs`

```csharp
// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,      // ⏰ Checks expiration
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),
            ClockSkew = TimeSpan.Zero  // No grace period
        };
    });

builder.Services.AddAuthorization();

// In middleware pipeline
app.UseAuthentication();  // Validates JWT tokens
app.UseAuthorization();   // Checks [Authorize] attributes
```

**Protected Controller Example:**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // 🔒 Requires valid JWT token
public class ReportsController : ControllerBase
{
    [HttpGet("yearly-summaries")]
    public async Task<IActionResult> GetYearlySummaries()
    {
        // Get userId from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();
            
        var userId = int.Parse(userIdClaim);
        
        // Fetch user-specific data
        var summaries = await _reportService.GetYearlySummariesAsync(userId);
        return Ok(summaries);
    }
}
```

**What happens when JWT is invalid:**
1. `[Authorize]` attribute checks token
2. Token validation fails (expired, invalid signature, wrong issuer, etc.)
3. Returns **401 Unauthorized** automatically
4. ApiService catches 401 → throws `UnauthorizedAccessException`
5. Page component catches exception → redirects to login

---

### **3. Authentication Failure Scenarios & Redirects**

#### **Scenario 1: No Token (User Not Logged In)**

```
User navigates to /reports directly
       ↓
Reports.razor.OnInitializedAsync()
       ↓
AuthStateProvider.IsAuthenticated() returns FALSE
       ↓
NavigationManager.NavigateTo("/login", forceLoad: true)
       ↓
User sees login page
No API call made (prevented at UI level)
```

**Code Flow:**
```csharp
if (!AuthStateProvider.IsAuthenticated())
{
    NavigationManager.NavigateTo("/login", forceLoad: true);
    return; // Stops execution
}
```

---

#### **Scenario 2: Expired Token (Session Timeout)**

```
User logged in 9 hours ago (token expired after 8 hours)
User navigates to /reports
       ↓
AuthStateProvider.IsAuthenticated() returns TRUE
  (Circuit still has old token in memory)
       ↓
Component attempts to load data
       ↓
ApiService.GetAsync("/api/reports/yearly-summaries")
  EnsureToken() → Injects expired token
       ↓
API receives request with expired token
       ↓
JWT middleware validates token
  Token lifetime check FAILS (expired)
       ↓
Returns 401 Unauthorized
       ↓
ApiService catches 401
  throws UnauthorizedAccessException
       ↓
Component catches UnauthorizedAccessException
  NavigationManager.NavigateTo("/login", forceLoad: true)
       ↓
User redirected to login page
Error message: "Session expired. Please log in again."
```

**Code Flow:**
```csharp
try
{
    var data = await ApiService.GetAsync<Data>("/api/endpoint");
}
catch (UnauthorizedAccessException ex)
{
    // Session expired during API call
    NavigationManager.NavigateTo("/login", forceLoad: true);
}
```

---

#### **Scenario 3: Invalid Token (Tampered or Corrupted)**

```
User manually modifies token in browser dev tools
OR
Token corrupted during transmission
       ↓
API receives request with invalid token
       ↓
JWT middleware validates signature
  Signature verification FAILS
       ↓
Returns 401 Unauthorized
       ↓
Same flow as expired token scenario
       ↓
User redirected to /login
```

---

#### **Scenario 4: Token Validation Failure (Wrong Issuer/Audience)**

```
API configuration changed (different JWT secret)
User has old token from previous deployment
       ↓
API receives request
       ↓
JWT middleware validates token
  Issuer/Audience/Key mismatch
       ↓
Returns 401 Unauthorized
       ↓
User redirected to /login
```

---

### **4. Why `forceLoad: true`?**

```csharp
NavigationManager.NavigateTo("/login", forceLoad: true);
```

**Without `forceLoad: true`:**
- Blazor navigates within the current circuit
- Old authentication state might persist
- Component state not fully reset

**With `forceLoad: true`:**
- Forces full page reload
- Destroys current SignalR circuit
- Creates new circuit with fresh state
- Ensures clean authentication state

**Critical for:**
- Logout (must clear all state)
- Session expiration (prevent stale data)
- Authentication failures (ensure clean slate)

---

### **5. Complete Login Flow Diagram**

```
┌─────────────────────────────────────────────────────┐
│                    USER ACTIONS                      │
└───────────────────┬─────────────────────────────────┘
                    │
     ┌──────────────┴──────────────┐
     ↓                             ↓
┌─────────┐                  ┌──────────┐
│  Login  │                  │ Navigate │
│  Page   │                  │ to Page  │
└────┬────┘                  └────┬─────┘
     │                            │
     │ Submit credentials         │ OnInitializedAsync()
     ↓                            ↓
┌─────────────────┐      ┌──────────────────┐
│  ApiService     │      │ Auth Check       │
│  POST /login    │      │ IsAuthenticated? │
└────┬────────────┘      └────┬─────────────┘
     │                        │
     │                   ┌────┴────┐
     │                   ↓         ↓
     │               [TRUE]    [FALSE]
     │                   │         │
     ↓                   │         ↓
┌─────────────┐         │    Redirect /login
│ API Backend │         │
│ Validates   │         │
└────┬────────┘         │
     │                  │
┌────┴────┐             │
↓         ↓             │
[VALID] [INVALID]       │
  │         │           │
  │         └──────┐    │
  ↓                ↓    ↓
Generate        401   Load
JWT Token      Error  Data
  │                │    │
  ↓                │    ↓
Return Token       │   API Call
  │                │    │
  ↓                │    ├────────┐
Store in           │    ↓        ↓
AuthState          │  [200]   [401]
  │                │    │        │
  ↓                │    ↓        ↓
Set Header         │  Show    Session
Bearer Token       │  Data    Expired
  │                │           │
  ↓                │           │
Navigate Home      │           │
  │                │           │
  └────────────────┴───────────┘
                   ↓
             Redirect /login
```

### **Login Redirect Scenarios** 🚨

**The system redirects to login page in these situations:**

#### **1. Accessing Protected Pages Without Login**
**Trigger:** User directly navigates to protected page  
**Pages Affected:**
- `/submit-income` - Income submission
- `/reports` - Tax reports
- `/ledger` - General ledger
- `/logout` - Logout page

**Implementation:**
```csharp
protected override async Task OnInitializedAsync()
{
    if (!AuthStateProvider.IsAuthenticated())
    {
        NavigationManager.NavigateTo("/login", forceLoad: true);
        return;
    }
    // Load protected content
}
```

**User Experience:**
- Immediate redirect to `/login`
- No error message shown (expected behavior)
- After login, user must manually navigate to desired page

---

#### **2. Token Expiration During Active Session**
**Trigger:** JWT token expires after 8 hours  
**When:** User makes API request with expired token  
**Response:** HTTP 401 Unauthorized

**Implementation:**
```csharp
// In ApiService
if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    throw new UnauthorizedAccessException("Session expired");
}

// In Page Component
try 
{
    await ApiService.GetAsync<Data>("/api/endpoint");
}
catch (UnauthorizedAccessException)
{
    NavigationManager.NavigateTo("/login", forceLoad: true);
}
```

**User Experience:**
- Error caught during API call
- Automatic redirect to `/login`
- User sees: "Session expired. Please log in again."

---

#### **3. Invalid or Malformed Token**
**Trigger:** Token tampering or corruption  
**When:** Token validation fails  
**Response:** HTTP 401 Unauthorized

**Implementation:** Same as token expiration  
**User Experience:** Redirect to `/login` with error message

---

#### **4. Manual Logout**
**Trigger:** User clicks "Logout" button  
**Process:**
1. Navigate to `/logout` page
2. `AuthStateProvider.MarkUserAsLoggedOut()`
3. `ApiService.ClearToken()`
4. Redirect to `/login`

**Implementation:**
```csharp
// Logout.razor
protected override void OnInitialized()
{
    AuthStateProvider.MarkUserAsLoggedOut();
    ApiService.ClearToken();
    NavigationManager.NavigateTo("/login", forceLoad: true);
}
```

**User Experience:**
- Clean logout
- Authentication state cleared
- Confirmation message: "You have been logged out"

---

#### **5. Concurrent Login on Different Device**
**Current Behavior:** NOT implemented  
**Recommendation:** Add token revocation or single-session enforcement

---

### **Protected vs Public Pages**

| Page | Route | Authentication | Redirect Behavior |
|------|-------|----------------|-------------------|
| Home | `/` | Public | No redirect |
| Calculator | `/calculator` | Public | No redirect |
| Login | `/login` | Public | No redirect |
| Register | `/register` | Public | No redirect |
| Submit Income | `/submit-income` | **Required** | → `/login` |
| Reports | `/reports` | **Required** | → `/login` |
| Ledger | `/ledger` | **Required** | → `/login` |
| Logout | `/logout` | N/A | → `/login` |

---

### **Security Features**

1. **Password Requirements:**
   - Minimum 8 characters
   - At least 1 uppercase letter
   - At least 1 lowercase letter
   - At least 1 digit

2. **Token Security:**
   - Signed with secret key
   - Issuer and audience validation
   - Expiration enforcement
   - Stored in memory (circuit-scoped)

3. **API Protection:**
   - `[Authorize]` attribute on protected controllers
   - Automatic token validation
   - Consistent 401 responses

4. **Input Validation:**
   - Username: 3-50 chars, alphanumeric + underscore
   - Email: RFC 5322 compliant regex
   - Password: Strength validation
   - All amounts: Non-negative, reasonable limits

---

## 💼 Core Business Features

### **1. Tax Calculation Engine**

**Nigeria Tax Act 2025 - Progressive Tax Brackets:**

| Income Range | Rate | Example |
|-------------|------|---------|
| ₦0 - ₦800,000 | 0% | First ₦800k = ₦0 tax |
| ₦800,001 - ₦3,000,000 | 15% | Next ₦2.2M = ₦330k tax |
| ₦3,000,001 - ₦12,000,000 | 18% | Next ₦9M = ₦1.62M tax |
| ₦12,000,001 - ₦25,000,000 | 21% | Next ₦13M = ₦2.73M tax |
| ₦25,000,001 - ₦50,000,000 | 23% | Next ₦25M = ₦5.75M tax |
| Above ₦50,000,000 | 25% | Above ₦50M = 25% |

**Key Features:**
- ✅ Real-time progressive calculation
- ✅ Bracket-by-bracket breakdown
- ✅ Effective tax rate computation
- ✅ Memory-cached brackets (24-hour TTL)
- ✅ Automatic CRA (Consolidated Relief Allowance)

**Algorithm:**
```csharp
For each tax bracket:
    If taxableIncome > bracket.MinIncome:
        incomeInBracket = Min(taxableIncome, bracket.MaxIncome) - bracket.MinIncome
        taxInBracket = incomeInBracket × bracket.TaxRate
        totalTax += taxInBracket

effectiveTaxRate = (totalTax / totalIncome) × 100
netIncome = totalIncome - totalTax
```

---

### **2. Income Management**

**Multiple Income Sources:**
- Employment (Salary, Bonuses, Commissions)
- Business Income
- Investment Income (Dividends, Interest)
- Rental Income
- Pension Income
- Freelance/Contract
- Royalties
- Other

**Advanced Features:**
- ✅ Monthly/Yearly toggle (automatic annualization)
- ✅ Monthly breakdown per source (12 months)
- ✅ Integration with General Ledger
- ✅ Fetch from previous year
- ✅ Import from ledger entries

**Monthly Breakdown Example:**
```
Income Source: Freelance Work
Annual Total: ₦6,000,000
Monthly Breakdown:
  Jan: ₦500,000  │  Jul: ₦450,000
  Feb: ₦600,000  │  Aug: ₦550,000
  Mar: ₦450,000  │  Sep: ₦500,000
  ... (tracks variable income)
```

---

### **3. Deduction System**

**Supported Deductions:**

| Type | Description | Max/Rules |
|------|-------------|-----------|
| **CRA** | Consolidated Relief Allowance | Auto: Higher of ₦200k or 20% |
| **NHF** | National Housing Fund | 2.5% of annual basic salary |
| **NHIS** | Health Insurance | Actual premiums paid |
| **Pension** | Pension Contributions | Up to 10% of income |
| **Insurance** | Life Insurance Premiums | Actual premiums paid |
| **Rent** | Rent Relief | 20% of annual rent, max ₦500k |
| **Mortgage** | Mortgage Interest | Actual interest paid |

**Features:**
- ✅ Multiple deductions per tax record
- ✅ Automatic CRA calculation
- ✅ Fetch from previous year
- ✅ Description field for each deduction

---

### **4. General Ledger**

**Financial Transaction Tracking:**

**Entry Types:**
- **Income** - All money received
- **Expense** - All money spent
- **Deduction** - Tax-deductible expenses

**Capabilities:**
- ✅ CRUD operations (Create, Read, Update, Delete)
- ✅ Category-based organization
- ✅ Date-range filtering
- ✅ Monthly aggregation (12-month view)
- ✅ Search by description
- ✅ Direct tax calculation from ledger
- ✅ Integration with tax submissions

**Monthly Summary View:**
```
January 2025:
  Income: ₦500,000
  Expenses: ₦200,000
  Net: ₦300,000

February 2025:
  Income: ₦550,000
  Expenses: ₦220,000
  Net: ₦330,000
```

---

### **5. Reporting & Analytics**

**Report Types:**

1. **Tax Summary** - Single year overview
   - Total income, deductions, taxable income
   - Total tax, effective rate, net income
   - Last updated timestamp

2. **User Report** - Multi-year history
   - Year-over-year comparison
   - Income trends
   - Tax paid trends

3. **Yearly Summaries** - All years at a glance
   - Quick summary cards
   - Key metrics per year
   - Edit/view actions

**Features:**
- ✅ Expandable details per year
- ✅ Breakdown by income source
- ✅ Breakdown by deduction
- ✅ Export-ready format
- ✅ Mobile-responsive cards

---

## 🗄️ Database Design

### **Entity Relationship Diagram**

```
User (1) ──────────┬──────────> (N) TaxRecord
                   │
                   └──────────> (N) GeneralLedger

TaxRecord (1) ─────┬──────────> (N) IncomeSource
                   │
                   └──────────> (N) Deduction

IncomeSource (1) ──────────────> (N) MonthlyIncome
```

### **Key Tables**

**Users:**
```sql
Id, Username (unique), Email (unique), PasswordHash, 
FullName, CreatedAt, LastLoginAt, IsActive
```

**TaxRecords:**
```sql
Id, UserId (FK), TaxYear, TotalIncome, TaxableIncome,
TotalTax, EffectiveTaxRate, IsProcessed, CreatedAt, UpdatedAt
```

**IncomeSources:**
```sql
Id, TaxRecordId (FK), SourceType, Description, 
Amount, MonthlyBreakdownEnabled
```

**MonthlyIncome:**
```sql
Id, IncomeSourceId (FK), Month (1-12), Amount
```

**Deductions:**
```sql
Id, TaxRecordId (FK), DeductionType, Description, Amount
```

**GeneralLedger:**
```sql
Id, UserId (FK), EntryDate, Description, Category,
Amount, EntryType, CreatedAt, UpdatedAt
```

### **Database Features**

- ✅ Cascade deletes for referential integrity
- ✅ Unique constraints on usernames/emails
- ✅ Decimal precision (18,2) for monetary values
- ✅ Composite indexes for performance
- ✅ Navigation properties for EF Core relationships
- ✅ Audit fields (CreatedAt, UpdatedAt)

---

## 🔌 API Design

### **Endpoint Overview**

**Authentication (Public):**
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token

**Tax Management (Protected):**
- `POST /api/tax/submit` - Submit income and deductions
- `GET /api/tax/summary/{year}` - Get tax summary
- `GET /api/tax/records` - Get all tax records
- `GET /api/tax/brackets` - Get tax brackets (public)

**General Ledger (Protected):**
- `POST /api/ledger/entry` - Add ledger entry
- `PUT /api/ledger/entry/{id}` - Update entry
- `DELETE /api/ledger/entry/{id}` - Delete entry
- `GET /api/ledger/summary` - Get ledger summary
- `GET /api/ledger/monthly-summary/{year}` - Monthly breakdown
- `POST /api/ledger/calculate-tax` - Calculate tax from ledger

**Reports (Protected):**
- `GET /api/reports/user` - Get user report
- `GET /api/reports/yearly-summaries` - Get all summaries

**Health (Public):**
- `GET /api/health` - Basic health check
- `GET /api/health/detailed` - Detailed status

### **API Design Principles**

- ✅ RESTful conventions (resource-based URLs)
- ✅ Proper HTTP verbs (GET, POST, PUT, DELETE)
- ✅ Consistent JSON responses
- ✅ Proper status codes (200, 400, 401, 404, 500)
- ✅ Error messages in response body
- ✅ JWT Bearer authentication header
- ✅ CORS enabled for cross-origin requests

---

## 🎨 Frontend Architecture

### **Blazor Server Structure**

```
Components/
├── Pages/
│   ├── Home.razor (Public)
│   ├── Calculator.razor (Public)
│   ├── Login.razor (Public)
│   ├── Register.razor (Public)
│   ├── SubmitIncome.razor (Protected)
│   ├── Reports.razor (Protected)
│   ├── Ledger.razor (Protected)
│   └── Logout.razor (Public)
└── Layout/
    ├── MainLayout.razor
    └── NavMenu.razor

Services/
├── ApiService.cs (HTTP client wrapper)
├── AuthStateProvider.cs (Auth state management)
└── AuthStateCache.cs (Session caching)

wwwroot/
└── css/
    └── app.css (Custom styles)
```

### **UI/UX Features**

**Design Principles:**
- ✅ Mobile-first responsive (Bootstrap 5)
- ✅ Loading states with spinners
- ✅ Real-time form validation
- ✅ Error boundaries with user-friendly messages
- ✅ Confirmation dialogs for destructive actions
- ✅ Toast notifications for feedback
- ✅ Card-based layouts
- ✅ Accessible forms (ARIA labels)

**Component Patterns:**
- Reusable form components
- Modal dialogs
- Alert banners
- Summary cards
- Data tables with filtering

---

## 🛠️ Technology Stack

### **Backend**
- **Framework:** ASP.NET Core 9.0
- **Language:** C# 12
- **ORM:** Entity Framework Core 9.0
- **Database:** SQLite (Dev), SQL Server (Prod)
- **Authentication:** JWT Bearer
- **Logging:** Serilog (Console + File)
- **Caching:** In-Memory Cache

### **Frontend**
- **Framework:** Blazor Server
- **UI Library:** Bootstrap 5
- **Real-time:** SignalR
- **Icons:** Bootstrap Icons
- **HTTP Client:** HttpClientFactory

### **Development**
- **.NET SDK:** 9.0
- **IDE:** Visual Studio / VS Code / Rider
- **Version Control:** Git
- **Package Manager:** NuGet

---

## 🚀 Deployment

### **Configuration Files**

**API (appsettings.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=nasosotax.db"
  },
  "Jwt": {
    "Key": "SecretKey32CharsMinimum...",
    "Issuer": "NasosoTax",
    "Audience": "NasosoTaxUsers"
  }
}
```

**Web (appsettings.json):**
```json
{
  "ApiBaseUrl": "http://localhost:5001"
}
```

### **Deployment Options**

**Option 1: Local/Development**
```bash
# Terminal 1
cd NasosoTax.Api
dotnet run  # Port 5001

# Terminal 2
cd NasosoTax.Web
dotnet run  # Port 5070
```

**Option 2: IIS (Windows Server)**
- Publish both projects
- Configure IIS sites
- Update API URL in Web config

**Option 3: Azure (Cloud)**
- Azure App Service (API + Web)
- Azure SQL Database
- Application Insights
- Azure Key Vault for secrets

**Option 4: Docker**
```dockerfile
# Dockerfile.api
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY ./publish /app
ENTRYPOINT ["dotnet", "NasosoTax.Api.dll"]

# Dockerfile.web
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY ./publish /app
ENTRYPOINT ["dotnet", "NasosoTax.Web.dll"]
```

---

## 📊 Performance Considerations

### **Implemented Optimizations**

1. **Caching:**
   - Memory cache for tax brackets (24h TTL)
   - Circuit-scoped services (Blazor)
   - HTTP client factory

2. **Database:**
   - Eager loading for relationships
   - Projections for large queries
   - AsNoTracking for read-only queries

3. **Logging:**
   - Structured logging (Serilog)
   - Async file writing
   - Log level filtering

### **Recommended Improvements**

1. **Database Indexes:**
   ```sql
   CREATE INDEX IX_Users_Email ON Users(Email);
   CREATE INDEX IX_Users_Username ON Users(Username);
   CREATE INDEX IX_TaxRecords_UserId_TaxYear ON TaxRecords(UserId, TaxYear);
   CREATE INDEX IX_GeneralLedgers_UserId_EntryDate ON GeneralLedgers(UserId, EntryDate);
   ```

2. **Response Compression:**
   ```csharp
   builder.Services.AddResponseCompression();
   ```

3. **Output Caching:**
   ```csharp
   app.MapGet("/api/tax/brackets")
      .CacheOutput(x => x.Expire(TimeSpan.FromHours(24)));
   ```

4. **Redis Caching:**
   - Distributed cache for multi-server deployments

5. **CDN:**
   - Static assets served from CDN

---

## 🧪 Testing Strategy (Recommended)

### **Test Pyramid**

```
        E2E Tests (10%)
       ▲
      / \
     /   \
    /     \
   /       \
  / Integration Tests (20%)
 /           \
/             \
────────────────
Unit Tests (70%)
```

### **Test Coverage Goals**

**Unit Tests (Priority):**
- TaxCalculationService - 100%
- ValidationHelper - 100%
- AuthService - 90%
- ReportService - 80%

**Integration Tests:**
- AuthController - All endpoints
- TaxController - All endpoints
- Database operations

**E2E Tests:**
- User registration and login
- Tax calculation flow
- Income submission flow

---

## 📈 Project Metrics

| Aspect | Rating | Notes |
|--------|--------|-------|
| **Architecture** | ⭐⭐⭐⭐⭐ | Perfect Clean Architecture |
| **Code Quality** | ⭐⭐⭐⭐⭐ | Well-organized, readable |
| **Security** | ⭐⭐⭐⭐ | JWT + validation, add 2FA |
| **Documentation** | ⭐⭐⭐⭐⭐ | Comprehensive guides |
| **Performance** | ⭐⭐⭐⭐ | Good, needs DB indexes |
| **Testing** | ⭐⭐ | Needs unit tests |
| **UI/UX** | ⭐⭐⭐⭐ | Professional, responsive |
| **API Design** | ⭐⭐⭐⭐⭐ | RESTful best practices |

**Overall:** ⭐⭐⭐⭐ (4/5 - Production Ready)

---

## 💡 Key Learnings for New Projects

### **What Makes This Project Excellent:**

1. **Clean Architecture** - Proper separation enables independent testing, technology swapping, and scalability

2. **Complete Separation** - Frontend and backend can scale independently and be deployed separately

3. **Security First** - JWT authentication, password hashing, input validation from day one

4. **User Experience** - Real-time feedback, loading states, clear error messages, responsive design

5. **Production Ready** - Logging, error handling, health checks, configuration management

### **Design Patterns to Replicate:**

- **Repository Pattern** - Clean data access abstraction
- **Service Layer** - Business logic separate from controllers
- **DTO Pattern** - API contracts separate from entities
- **Dependency Injection** - Loose coupling throughout
- **Middleware** - Cross-cutting concerns (errors, logging)
- **Circuit-Scoped Services** - Blazor Server state management

### **Common Pitfalls Avoided:**

❌ **Avoided:** Mixing business logic in controllers  
✅ **Done:** Separated into service layer

❌ **Avoided:** Direct database access from UI  
✅ **Done:** Repository pattern with abstraction

❌ **Avoided:** Storing passwords in plain text  
✅ **Done:** PBKDF2 hashing with salt

❌ **Avoided:** No input validation  
✅ **Done:** Comprehensive validation on all inputs

❌ **Avoided:** Poor error handling  
✅ **Done:** Global middleware + consistent responses

---

## 🔄 Evolution & Roadmap

### **Phase 1 - Completed ✅**
- Clean Architecture implementation
- Frontend/Backend separation
- JWT authentication
- Tax calculation engine
- General Ledger
- Comprehensive documentation

### **Phase 2 - Recommended (1-2 months)**
- [ ] Unit test coverage (80%+)
- [ ] Database indexing
- [ ] Swagger/OpenAPI documentation
- [ ] Client-side caching
- [ ] Rate limiting

### **Phase 3 - Future (3-6 months)**
- [ ] Mobile app (same API)
- [ ] Advanced reporting with charts
- [ ] Export to PDF/Excel
- [ ] Multi-currency support
- [ ] Email notifications

### **Phase 4 - Advanced (6+ months)**
- [ ] Machine learning tax predictions
- [ ] External integrations (banks, accounting software)
- [ ] Multi-tenant support
- [ ] Microservices architecture

---

## 📚 Use This Project as Reference For:

### **1. Architecture & Design**
✅ Clean Architecture implementation  
✅ Layer separation and dependency management  
✅ Frontend/Backend communication patterns  
✅ Project structure organization  

### **2. Authentication & Security**
✅ JWT token implementation  
✅ Blazor Server authentication  
✅ Protected routes and endpoints  
✅ Password hashing (PBKDF2)  
✅ Input validation patterns  

### **3. API Development**
✅ RESTful API design  
✅ Controller structure  
✅ Error handling middleware  
✅ Consistent response formats  
✅ Health check endpoints  

### **4. Database & Data Access**
✅ Entity Framework Core setup  
✅ Code-first migrations  
✅ Repository pattern  
✅ Entity relationships  
✅ Query optimization  

### **5. Business Logic**
✅ Complex calculations (progressive tax)  
✅ Service layer patterns  
✅ Data aggregation  
✅ Validation logic  

### **6. Frontend Development**
✅ Blazor Server architecture  
✅ Component structure  
✅ Form handling and validation  
✅ Real-time updates  
✅ Responsive design with Bootstrap  

### **7. DevOps & Deployment**
✅ Multi-environment configuration  
✅ Logging with Serilog  
✅ Health checks  
✅ Database migrations  

---

## 🎯 Quick Start for Learning

### **If You Want to Learn:**

**Clean Architecture:**
→ Study layer structure, dependencies flow toward Domain

**Authentication:**
→ Follow login flow from UI → API → Database → Token generation

**API Design:**
→ Examine controller methods, response patterns, error handling

**Database Design:**
→ Study entity relationships, EF Core configuration, migrations

**Blazor Server:**
→ Explore page components, services, state management

**Business Logic:**
→ TaxCalculationService - complex progressive tax algorithm

---

## 📞 Project Information

**Repository:** github.com/KelvinEsiri/NasosoTax  
**Author:** Kelvin Esiri  
**License:** MIT  
**Version:** 1.1.0  
**Last Updated:** October 15, 2025  
**Status:** ✅ Production Ready  

---

## 📝 Final Notes

This project demonstrates enterprise-grade software development with:
- **Clean Architecture** for maintainability
- **Modern Security** practices
- **RESTful API** design
- **Comprehensive Documentation**
- **Production-Ready** code

**Use this as a reference template for building scalable, maintainable, and secure web applications.**

---

**Document Version:** 1.0  
**Created:** October 15, 2025  
**For:** Project design reference and learning purposes
