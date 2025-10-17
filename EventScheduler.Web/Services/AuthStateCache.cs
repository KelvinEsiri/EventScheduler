using System.Collections.Concurrent;

namespace EventScheduler.Web.Services;

/// <summary>
/// Singleton cache that persists authentication state across circuit recreations.
/// Survives SignalR disconnections but NOT server restarts (in-memory only).
/// Implements the NasosoTax three-tier persistence strategy.
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
    public void SetAuthState(string sessionId, string username, string email, int userId, string token)
    {
        var state = new CachedAuthState
        {
            Username = username,
            Email = email,
            UserId = userId,
            Token = token,
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8) // Match JWT expiration
        };

        _cache[sessionId] = state;
        
        _logger.LogInformation(
            "[AuthStateCache] Auth state cached for session {SessionId}: {Username} (UserId: {UserId})", 
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
                    "[AuthStateCache] Auth state retrieved for session {SessionId}: {Username}", 
                    sessionId, state.Username
                );
                return state;
            }
            else
            {
                // Expired - remove from cache
                _cache.TryRemove(sessionId, out _);
                _logger.LogInformation(
                    "[AuthStateCache] Auth state expired for session {SessionId}, removed from cache", 
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
                "[AuthStateCache] Auth state cleared for session {SessionId}", 
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
                "[AuthStateCache] Cleaned up {Count} expired auth cache entries", 
                expiredKeys.Count
            );
        }
    }

    /// <summary>
    /// Gets the current cache size for monitoring
    /// </summary>
    public int GetCacheSize()
    {
        return _cache.Count;
    }
}

/// <summary>
/// Cached authentication state
/// </summary>
public class CachedAuthState
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
