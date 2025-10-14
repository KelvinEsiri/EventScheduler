using EventScheduler.Application.DTOs.Request;
using EventScheduler.Application.DTOs.Response;
using EventScheduler.Application.Interfaces.Repositories;
using EventScheduler.Application.Interfaces.Services;
using EventScheduler.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EventScheduler.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IEmailService emailService, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate
        if (await _userRepository.ExistsAsync(request.Username, request.Email))
        {
            throw new InvalidOperationException("Username or email already exists");
        }

        // Hash password
        var passwordHash = HashPassword(request.Password);

        // Create user
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow,
            EmailVerified = false
        };

        var createdUser = await _userRepository.CreateAsync(user);

        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(createdUser.Email, createdUser.FullName);

        // Generate token
        var token = GenerateJwtToken(createdUser.Id, createdUser.Username, createdUser.Email);

        return new LoginResponse
        {
            Token = token,
            Username = createdUser.Username,
            Email = createdUser.Email,
            FullName = createdUser.FullName,
            UserId = createdUser.Id
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var token = GenerateJwtToken(user.Id, user.Username, user.Email);

        return new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            UserId = user.Id
        };
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        // Generate reset token
        var resetToken = GenerateResetToken();
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _userRepository.UpdateAsync(user);
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken, user.FullName);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _userRepository.GetByPasswordResetTokenAsync(token);
        
        if (user == null || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            return false;
        }

        user.PasswordHash = HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _userRepository.UpdateAsync(user);

        return true;
    }

    public string GenerateJwtToken(int userId, string username, string email)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "EventScheduler";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "EventScheduler";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim("userId", userId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string HashPassword(string password)
    {
        using var rfc2898 = new Rfc2898DeriveBytes(password, 16, 10000, HashAlgorithmName.SHA256);
        byte[] hash = rfc2898.GetBytes(32);
        byte[] salt = rfc2898.Salt;

        byte[] hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }

    private bool VerifyPassword(string password, string hashedPassword)
    {
        byte[] hashBytes = Convert.FromBase64String(hashedPassword);
        byte[] salt = new byte[16];
        Array.Copy(hashBytes, 0, salt, 0, 16);

        using var rfc2898 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
        byte[] hash = rfc2898.GetBytes(32);

        for (int i = 0; i < 32; i++)
        {
            if (hashBytes[i + 16] != hash[i])
            {
                return false;
            }
        }

        return true;
    }

    private string GenerateResetToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
