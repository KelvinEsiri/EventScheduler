namespace EventScheduler.Application.DTOs.Response;

public class LoginResponse
{
    public required string Token { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public int UserId { get; set; }
}
