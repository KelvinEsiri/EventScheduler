using Microsoft.AspNetCore.SignalR;

namespace EventScheduler.Api.Hubs;

/// <summary>
/// SignalR hub for real-time event notifications
/// Handles WebSocket connections and broadcasts event changes to connected clients
/// </summary>
public class EventHub : Hub
{
    private readonly ILogger<EventHub> _logger;

    public EventHub(ILogger<EventHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// Logs connection details for monitoring and debugging
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("✅ SignalR: Client connected!");
        _logger.LogInformation("Connection ID: {ConnectionId}", Context.ConnectionId);
        _logger.LogInformation("User: {User}", Context.User?.Identity?.Name ?? "Anonymous");
        _logger.LogInformation("========================================");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// Logs disconnection details and any errors that occurred
    /// </summary>
    /// <param name="exception">Exception that caused the disconnection, if any</param>
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
    /// Users can join groups to receive targeted notifications
    /// </summary>
    /// <param name="userId">The user ID to create a group for</param>
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        _logger.LogInformation("User {UserId} joined their group", userId);
    }

    /// <summary>
    /// Leave a user-specific group
    /// Called when a user no longer wants to receive notifications for a specific group
    /// </summary>
    /// <param name="userId">The user ID to leave the group for</param>
    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        _logger.LogInformation("User {UserId} left their group", userId);
    }
}
