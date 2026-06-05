using System.ComponentModel.DataAnnotations;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class LocalizedRangeAttribute : RangeAttribute
{
    public string MessageKey { get; set; } = string.Empty;

    public LocalizedRangeAttribute(double minimum, double maximum)
        : base(minimum, maximum)
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var result = base.IsValid(value, validationContext);

        if (result == ValidationResult.Success)
        {
            return result;
        }

        var localizer = (LocalizationService?)validationContext.GetService(typeof(LocalizationService));
        var message = localizer?.Get(MessageKey) ?? MessageKey;
        return new ValidationResult(message);
    }
}
