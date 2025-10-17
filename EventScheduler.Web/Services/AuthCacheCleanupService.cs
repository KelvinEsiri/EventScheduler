namespace EventScheduler.Web.Services;

/// <summary>
/// Background service that periodically cleans up expired authentication cache entries
/// Prevents memory leaks and maintains cache health
/// </summary>
public class AuthCacheCleanupService : BackgroundService
{
    private readonly ILogger<AuthCacheCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AuthCacheCleanupService(
        ILogger<AuthCacheCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[AuthCacheCleanup] Background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run cleanup every 30 minutes
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);

                // Get singleton cache instance
                var cache = _serviceProvider.GetRequiredService<AuthStateCache>();
                
                var cacheSize = cache.GetCacheSize();
                _logger.LogInformation("[AuthCacheCleanup] Auth cache size before cleanup: {Size} entries", cacheSize);

                // Clean up expired entries
                cache.CleanupExpiredEntries();

                var newCacheSize = cache.GetCacheSize();
                _logger.LogInformation("[AuthCacheCleanup] Auth cache size after cleanup: {Size} entries", newCacheSize);

                // Warn if cache is very large
                if (newCacheSize > 10000)
                {
                    _logger.LogWarning(
                        "[AuthCacheCleanup] Auth cache is very large: {Size} entries. Consider implementing distributed caching.",
                        newCacheSize
                    );
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthCacheCleanup] Error during cleanup");
            }
        }

        _logger.LogInformation("[AuthCacheCleanup] Background service stopped");
    }
}
