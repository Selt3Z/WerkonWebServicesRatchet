using System.ComponentModel.DataAnnotations;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class LocalizedRequiredAttribute : ValidationAttribute
{
    public string MessageKey { get; set; } = string.Empty;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var isValid = value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => true
        };

        if (isValid)
        {
            return ValidationResult.Success;
        }

        var localizer = (LocalizationService?)validationContext.GetService(typeof(LocalizationService));
        var message = localizer?.Get(MessageKey) ?? MessageKey;
        return new ValidationResult(message);
    }
}
