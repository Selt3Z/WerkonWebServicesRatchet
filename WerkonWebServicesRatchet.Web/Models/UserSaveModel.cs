using WerkonWebServicesRatchet.Web.Services;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class UserSaveModel
{
    [LocalizedRequired(MessageKey = "Validation_UserNameRequired")]
    public string UserName { get; set; } = string.Empty;

    public string? Password { get; set; }

    [LocalizedRequired(MessageKey = "Validation_DisplayNameRequired")]
    public string DisplayName { get; set; } = string.Empty;

    [LocalizedRequired(MessageKey = "Validation_RoleRequired")]
    public string Role { get; set; } = string.Empty;
}
