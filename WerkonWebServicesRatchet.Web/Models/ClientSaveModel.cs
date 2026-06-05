using WerkonWebServicesRatchet.Web.Services;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class ClientSaveModel
{
    [LocalizedRequired(MessageKey = "Validation_FullNameRequired")]
    public string FullName { get; set; } = string.Empty;

    [LocalizedRequired(MessageKey = "Validation_PhoneRequired")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
