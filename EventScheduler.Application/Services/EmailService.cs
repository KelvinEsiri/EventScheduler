using EventScheduler.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace EventScheduler.Application.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(string email, string fullName)
    {
        _logger.LogInformation("Sending welcome email to {Email} ({FullName})", email, fullName);
        
        // In a real application, this would send an actual email
        // For now, we'll just log it
        await Task.Run(() =>
        {
            _logger.LogInformation(
                "Welcome Email:\n" +
                "To: {Email}\n" +
                "Subject: Welcome to Event Scheduler!\n" +
                "Body: Hello {FullName}, Welcome to Event Scheduler. Your account has been created successfully.",
                email, fullName
            );
        });
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string fullName)
    {
        _logger.LogInformation("Sending password reset email to {Email} ({FullName})", email, fullName);
        
        await Task.Run(() =>
        {
            _logger.LogInformation(
                "Password Reset Email:\n" +
                "To: {Email}\n" +
                "Subject: Password Reset Request\n" +
                "Body: Hello {FullName}, You requested a password reset. Use this token: {Token}",
                email, fullName, resetToken
            );
        });
    }

    public async Task SendEventReminderEmailAsync(string email, string fullName, string eventTitle, DateTime eventDate)
    {
        _logger.LogInformation("Sending event reminder email to {Email} for event {EventTitle}", email, eventTitle);
        
        await Task.Run(() =>
        {
            _logger.LogInformation(
                "Event Reminder Email:\n" +
                "To: {Email}\n" +
                "Subject: Reminder: {EventTitle}\n" +
                "Body: Hello {FullName}, This is a reminder that your event is scheduled for {EventDate}.",
                email, eventTitle, fullName, eventDate
            );
        });
    }

    public async Task SendEventCompletedEmailAsync(string email, string fullName, string eventTitle)
    {
        _logger.LogInformation("Sending event completed notification to {Email} for event {EventTitle}", email, eventTitle);
        
        await Task.Run(() =>
        {
            _logger.LogInformation(
                "Event Completed Email:\n" +
                "To: {Email}\n" +
                "Subject: Event Completed: {EventTitle}\n" +
                "Body: Hello {FullName}, Your event has been marked as completed.",
                email, eventTitle, fullName
            );
        });
    }
}
