namespace EventScheduler.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email, string fullName);
    Task SendPasswordResetEmailAsync(string email, string resetToken, string fullName);
    Task SendEventReminderEmailAsync(string email, string fullName, string eventTitle, DateTime eventDate);
    Task SendEventCompletedEmailAsync(string email, string fullName, string eventTitle);
}
