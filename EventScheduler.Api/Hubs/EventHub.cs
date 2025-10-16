using Microsoft.AspNetCore.SignalR;

namespace EventScheduler.Api.Hubs;

/// <summary>
/// SignalR hub for real-time event notifications
/// </summary>
public class EventHub : Hub
{
    private readonly ILogger<EventHub> _logger;

    public EventHub(ILogger<EventHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("✅ SignalR: Client connected!");
        _logger.LogInformation("Connection ID: {ConnectionId}", Context.ConnectionId);
        _logger.LogInformation("User: {User}", Context.User?.Identity?.Name ?? "Anonymous");
        _logger.LogInformation("========================================");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "❌ SignalR: Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("SignalR: Client disconnected normally: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a user-specific group to receive notifications
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        _logger.LogInformation("User {UserId} joined their group", userId);
    }

    /// <summary>
    /// Leave a user-specific group
    /// </summary>
    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        _logger.LogInformation("User {UserId} left their group", userId);
    }
}
