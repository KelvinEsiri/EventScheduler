using Microsoft.JSInterop;

namespace EventScheduler.Web.Services;

public class NetworkStatusService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<NetworkStatusService> _logger;
    private readonly IConfiguration _configuration;
    private bool _isOnline = true;
    private readonly List<Func<bool, Task>> _statusChangeHandlers = new();

    public NetworkStatusService(
        IJSRuntime jsRuntime, 
        ILogger<NetworkStatusService> logger,
        IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _configuration = configuration;
    }

    public bool IsOnline => _isOnline;

    public event Func<bool, Task>? OnStatusChanged;

    public async Task InitializeAsync()
    {
        try
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5006";
            var dotNetRef = DotNetObjectReference.Create(this);
            
            await _jsRuntime.InvokeVoidAsync("networkStatus.initialize", dotNetRef, apiBaseUrl);
            _isOnline = await _jsRuntime.InvokeAsync<bool>("networkStatus.isOnline");
            
            _logger.LogInformation("Network status service initialized. Online: {IsOnline}, API: {ApiUrl}", 
                _isOnline, apiBaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize network status service");
        }
    }

    [JSInvokable]
    public async Task UpdateNetworkStatus(bool isOnline)
    {
        if (_isOnline != isOnline)
        {
            _isOnline = isOnline;
            _logger.LogInformation("Network status changed: {Status}", isOnline ? "Online" : "Offline");

            if (OnStatusChanged != null)
            {
                await OnStatusChanged.Invoke(isOnline);
            }
        }
    }

    public async Task<bool> CheckConnectivityAsync()
    {
        try
        {
            _isOnline = await _jsRuntime.InvokeAsync<bool>("networkStatus.isOnline");
            return _isOnline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check network connectivity");
            return false;
        }
    }
    
    public async Task ForceServerHealthCheckAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("networkStatus.checkServerHealth");
            _logger.LogInformation("Manual server health check triggered");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger server health check");
        }
    }
}

