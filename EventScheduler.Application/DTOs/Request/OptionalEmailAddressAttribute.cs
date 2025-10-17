using System.ComponentModel.DataAnnotations;

namespace EventScheduler.Application.DTOs.Request;

/// <summary>
/// Validates email address format only if the value is not null or empty
/// </summary>
public class OptionalEmailAddressAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Allow null or empty values
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        // Validate email format if a value is provided
        var emailAttribute = new EmailAddressAttribute();
        if (!emailAttribute.IsValid(value))
        {
            return new ValidationResult(ErrorMessage ?? "Invalid email address format");
        }

        return ValidationResult.Success;
    }
}
