using System.ComponentModel.DataAnnotations;

namespace UberPrints.Server.Validation;

/// <summary>
/// Validation attribute that validates URLs only when the value is not null or empty.
/// This allows optional URL fields to be left blank without validation errors.
/// </summary>
public class OptionalUrlAttribute : ValidationAttribute
{
  protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
  {
    // Allow null or empty strings
    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
    {
      return ValidationResult.Success;
    }

    // Validate as URL if value is provided
    var urlAttribute = new UrlAttribute();
    if (!urlAttribute.IsValid(value))
    {
      return new ValidationResult(ErrorMessage ?? "The field must be a valid URL.");
    }

    return ValidationResult.Success;
  }
}
