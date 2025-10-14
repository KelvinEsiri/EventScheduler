using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;

namespace EventScheduler.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<bool> SendPasswordResetEmailAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    string GenerateJwtToken(int userId, string username, string email);
}
