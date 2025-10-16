using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventScheduler.Web.Services;

// Helper class for deserializing error responses
public class ErrorResponse
{
    public string? Error { get; set; }
}

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private string? _token;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public void SetToken(string token)
    {
        _token = token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        _token = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    // Auth endpoints
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

    // Event endpoints
    public async Task<List<EventResponse>> GetAllEventsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<EventResponse>>("/api/events") ?? new List<EventResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events");
            throw;
        }
    }

    public async Task<EventResponse?> GetEventByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EventResponse>($"/api/events/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event {EventId}", id);
            throw;
        }
    }

    public async Task<List<EventResponse>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<EventResponse>>(
                $"/api/events/date-range?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}") 
                ?? new List<EventResponse>();
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
            var response = await _httpClient.PostAsJsonAsync("/api/events", request);
            
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
                    
                    if (errorResponse?.Error != null)
                    {
                        throw new InvalidOperationException(errorResponse.Error);
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
            var response = await _httpClient.PutAsJsonAsync($"/api/events/{id}", request);
            
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
                    
                    if (errorResponse?.Error != null)
                    {
                        throw new InvalidOperationException(errorResponse.Error);
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
            var response = await _httpClient.DeleteAsync($"/api/events/{id}");
            response.EnsureSuccessStatusCode();
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
}
