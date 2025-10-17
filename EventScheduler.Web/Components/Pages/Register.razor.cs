using Microsoft.AspNetCore.Components;
using EventScheduler.Application.DTOs.Request;
using EventScheduler.Web.Services;

namespace EventScheduler.Web.Components.Pages;

public partial class Register
{
    [Inject] private ApiService ApiService { get; set; } = default!;
    [Inject] private AuthStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private RegisterRequest registerRequest = new() { Username = "", Email = "", Password = "", FullName = "" };
    private string? errorMessage;
    private bool isLoading = false;
    private int passwordStrength = 0;

    private void OnPasswordInput(ChangeEventArgs e)
    {
        var password = e.Value?.ToString() ?? "";
        registerRequest.Password = password;
        CalculatePasswordStrength(password);
    }

    private void CalculatePasswordStrength(string password)
    {
        passwordStrength = 0;
        if (string.IsNullOrEmpty(password)) return;

        if (password.Length >= 8) passwordStrength++;
        if (HasUpperCase(password)) passwordStrength++;
        if (HasLowerCase(password)) passwordStrength++;
        if (HasDigit(password)) passwordStrength++;
    }

    private bool HasUpperCase(string? password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsUpper);

    private bool HasLowerCase(string? password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsLower);

    private bool HasDigit(string? password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsDigit);

    private string GetPasswordStrengthClass()
    {
        return passwordStrength switch
        {
            0 => "strength-none",
            1 => "strength-weak",
            2 => "strength-fair",
            3 => "strength-good",
            4 => "strength-strong",
            _ => "strength-none"
        };
    }

    private string GetPasswordStrengthText()
    {
        return passwordStrength switch
        {
            0 => "Too weak",
            1 => "Weak",
            2 => "Fair",
            3 => "Good",
            4 => "Strong",
            _ => ""
        };
    }

    private async Task HandleRegister()
    {
        // Validate password strength
        if (passwordStrength < 3)
        {
            errorMessage = "Please choose a stronger password that meets all requirements.";
            return;
        }

        try
        {
            isLoading = true;
            errorMessage = null;

            var response = await ApiService.RegisterAsync(registerRequest);
            
            if (response != null)
            {
                AuthStateProvider.SetAuthentication(response.Username, response.Email, response.UserId, response.Token);
                ApiService.SetToken(response.Token);
                NavigationManager.NavigateTo("/calendar-view");
            }
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"Unable to connect to server. Please make sure the API is running. Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message.Contains("already exists") 
                ? "Registration failed. Username or email already exists." 
                : $"Registration failed: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }
}
