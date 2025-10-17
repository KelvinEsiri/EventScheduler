using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventScheduler.Web.Services;

/// <summary>
/// Helper class for deserializing API error responses
/// </summary>
public class ErrorResponse
{
    public string? Error { get; set; }
    public string? Title { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    
    public string GetErrorMessage()
    {
        // If we have validation errors, combine them into a readable message
        if (Errors != null && Errors.Any())
        {
            var messages = Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return string.Join("; ", messages);
        }
        
        // Otherwise return the simple error or title
        return Error ?? Title ?? "An error occurred";
    }
}

/// <summary>
/// Service for communicating with the EventScheduler API
/// Handles all HTTP requests to the backend API including authentication and event management
/// Follows NasosoTax reference patterns for token handling and error management
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private readonly AuthStateProvider _authStateProvider;
    private string? _token;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger, AuthStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authStateProvider = authStateProvider;
    }

    /// <summary>
    /// Sets the authentication token for subsequent API requests
    /// </summary>
    /// <param name="token">JWT bearer token</param>
    public void SetToken(string token)
    {
        _token = token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clears the authentication token (used during logout)
    /// </summary>
    public void ClearToken()
    {
        _token = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Ensures token is present in request headers before making API calls
    /// Automatically injects token from AuthStateProvider if not already set
    /// </summary>
    private void EnsureToken()
    {
        var token = _token ?? _authStateProvider.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>
    /// Checks if the response indicates an unauthorized access (401)
    /// Throws UnauthorizedAccessException which protected pages should catch and handle
    /// </summary>
    private void CheckUnauthorized(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Session expired. Please log in again.");
        }
    }

    #region Authentication Endpoints

    /// <summary>
    /// Registers a new user account
    /// </summary>
    /// <param name="request">Registration details including username, email, and password</param>
    /// <returns>Login response with user details and authentication token</returns>
    public async Task<LoginResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            throw;
        }
    }

    /// <summary>
    /// Authenticates a user and returns an authentication token
    /// </summary>
    /// <param name="request">Login credentials (username and password)</param>
    /// <returns>Login response with user details and authentication token</returns>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            throw;
        }
    }

    #endregion

    #region Event Management Endpoints

    /// <summary>
    /// Retrieves all events for the authenticated user
    /// </summary>
    /// <returns>List of user's events</returns>
    public async Task<List<EventResponse>> GetAllEventsAsync()
    {
        try
        {
            EnsureToken(); // Inject token into request
            
            // Add timeout to prevent hanging requests
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync("/api/events", cts.Token);
            CheckUnauthorized(response); // Check for 401
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<List<EventResponse>>() ?? new List<EventResponse>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request to get events was canceled or timed out");
            return new List<EventResponse>(); // Return empty list instead of throwing
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw to let calling component handle redirect
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a specific event by its ID
    /// </summary>
    /// <param name="id">The event ID</param>
    /// <returns>The event details or null if not found</returns>
    public async Task<EventResponse?> GetEventByIdAsync(int id)
    {
        try
        {
            EnsureToken();
            var response = await _httpClient.GetAsync($"/api/events/{id}");
            CheckUnauthorized(response);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<EventResponse>();
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event {EventId}", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all events within a specified date range
    /// </summary>
    /// <param name="startDate">Start date of the range</param>
    /// <param name="endDate">End date of the range</param>
    /// <returns>List of events within the date range</returns>
    public async Task<List<EventResponse>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            EnsureToken();
            var response = await _httpClient.GetAsync(
                $"/api/events/date-range?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
            CheckUnauthorized(response);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<List<EventResponse>>() ?? new List<EventResponse>();
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events by date range");
            throw;
        }
    }

    public async Task<EventResponse?> CreateEventAsync(CreateEventRequest request)
    {
        try
        {
            EnsureToken();
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await _httpClient.PostAsJsonAsync("/api/events", request, cts.Token);
            CheckUnauthorized(response);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error creating event. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                
                // Try to extract the error message from the response
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (errorResponse != null)
                    {
                        throw new InvalidOperationException(errorResponse.GetErrorMessage());
                    }
                }
                catch (JsonException)
                {
                    // If JSON deserialization fails, use the raw content
                }
                
                throw new HttpRequestException($"Error creating event: {response.StatusCode}");
            }
            
            return await response.Content.ReadFromJsonAsync<EventResponse>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request to create event was canceled or timed out");
            throw new InvalidOperationException("The request timed out. Please try again.");
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            // Re-throw validation errors
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            throw;
        }
    }

    public async Task<EventResponse?> UpdateEventAsync(int id, UpdateEventRequest request)
    {
        try
        {
            EnsureToken();
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await _httpClient.PutAsJsonAsync($"/api/events/{id}", request, cts.Token);
            CheckUnauthorized(response);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error updating event {EventId}. Status: {StatusCode}, Error: {Error}", 
                    id, response.StatusCode, errorContent);
                
                // Try to extract the error message from the response
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (errorResponse != null)
                    {
                        throw new InvalidOperationException(errorResponse.GetErrorMessage());
                    }
                }
                catch (JsonException)
                {
                    // If JSON deserialization fails, use the raw content
                }
                
                throw new HttpRequestException($"Error updating event: {response.StatusCode}");
            }
            
            return await response.Content.ReadFromJsonAsync<EventResponse>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request to update event was canceled or timed out");
            throw new InvalidOperationException("The request timed out. Please try again.");
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            // Re-throw validation errors
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", id);
            throw;
        }
    }

    public async Task DeleteEventAsync(int id)
    {
        try
        {
            EnsureToken();
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.DeleteAsync($"/api/events/{id}", cts.Token);
            CheckUnauthorized(response);
            response.EnsureSuccessStatusCode();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request to delete event was canceled or timed out");
            throw new InvalidOperationException("The request timed out. Please try again.");
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", id);
            throw;
        }
    }

    // Public event endpoints (no authentication required)
    public async Task<List<EventResponse>> GetPublicEventsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<EventResponse>>("/api/events/public") ?? new List<EventResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public events");
            throw;
        }
    }

    public async Task<EventResponse?> GetPublicEventByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EventResponse>($"/api/events/public/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public event {EventId}", id);
            throw;
        }
    }

    public async Task<EventResponse?> JoinPublicEventAsync(int id)
    {
        try
        {
            EnsureToken();
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.PostAsync($"/api/events/public/{id}/join", null, cts.Token);
            CheckUnauthorized(response);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<EventResponse>();
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining public event {EventId}", id);
            throw;
        }
    }

    public async Task LeaveEventAsync(int id)
    {
        try
        {
            EnsureToken();
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.PostAsync($"/api/events/public/{id}/leave", null, cts.Token);
            CheckUnauthorized(response);
            response.EnsureSuccessStatusCode();
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving event {EventId}", id);
            throw;
        }
    }

    #endregion
}
