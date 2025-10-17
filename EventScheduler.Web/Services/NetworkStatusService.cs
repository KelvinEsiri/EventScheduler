using Microsoft.JSInterop;

namespace EventScheduler.Web.Services;

/// <summary>
/// Service for monitoring network connectivity status
/// </summary>
public class NetworkStatusService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<NetworkStatusService> _logger;
    private bool _isOnline = true;
    private readonly List<Func<bool, Task>> _statusChangeHandlers = new();

    public NetworkStatusService(IJSRuntime jsRuntime, ILogger<NetworkStatusService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public bool IsOnline => _isOnline;

    public event Func<bool, Task>? OnStatusChanged;

    public async Task InitializeAsync()
    {
        try
        {
            var dotNetRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("networkStatus.initialize", dotNetRef);
            _isOnline = await _jsRuntime.InvokeAsync<bool>("networkStatus.isOnline");
            _logger.LogInformation("Network status service initialized. Online: {IsOnline}", _isOnline);
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
}
