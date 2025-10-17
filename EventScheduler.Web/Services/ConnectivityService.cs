using Microsoft.JSInterop;

namespace EventScheduler.Web.Services;

/// <summary>
/// Service for monitoring network connectivity and managing offline/online transitions
/// Wraps JavaScript connectivity manager and provides .NET interface
/// </summary>
public class ConnectivityService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ConnectivityService> _logger;
    private DotNetObjectReference<ConnectivityService>? _dotNetReference;
    private bool _isOnline = true;

    public event EventHandler<bool>? ConnectivityChanged;

    public ConnectivityService(IJSRuntime jsRuntime, ILogger<ConnectivityService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Initialize connectivity monitoring
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _dotNetReference = DotNetObjectReference.Create(this);
            _isOnline = await _jsRuntime.InvokeAsync<bool>(
                "connectivityManager.init", _dotNetReference);
            
            _logger.LogInformation("Connectivity service initialized. Status: {Status}", 
                _isOnline ? "ONLINE" : "OFFLINE");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize connectivity service");
        }
    }

    /// <summary>
    /// Get current connectivity status
    /// </summary>
    public bool IsOnline => _isOnline;

    /// <summary>
    /// Force a connectivity check
    /// </summary>
    public async Task<bool> CheckConnectivityAsync()
    {
        try
        {
            _isOnline = await _jsRuntime.InvokeAsync<bool>("connectivityManager.forceCheck");
            return _isOnline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check connectivity");
            return false;
        }
    }

    /// <summary>
    /// Called from JavaScript when connectivity changes
    /// </summary>
    [JSInvokable]
    public void OnConnectivityChanged(bool isOnline)
    {
        _logger.LogInformation("Connectivity changed: {Status}", isOnline ? "ONLINE" : "OFFLINE");
        _isOnline = isOnline;
        ConnectivityChanged?.Invoke(this, isOnline);
    }

    /// <summary>
    /// Called from JavaScript to trigger sync
    /// </summary>
    [JSInvokable]
    public async Task TriggerSync()
    {
        _logger.LogInformation("Sync triggered from JavaScript");
        // This will be implemented by the sync service
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("connectivityManager.dispose");
            _dotNetReference?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing connectivity service");
        }
    }
}
