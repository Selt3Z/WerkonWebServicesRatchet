using WerkonWebServicesRatchet.Web.Services;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class LoginModel
{
    [LocalizedRequired(MessageKey = "Validation_UserNameRequired")]
    public string UserName { get; set; } = string.Empty;

    [LocalizedRequired(MessageKey = "Validation_PasswordRequired")]
    public string Password { get; set; } = string.Empty;
}
