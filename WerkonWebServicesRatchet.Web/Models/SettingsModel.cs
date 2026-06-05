namespace WerkonWebServicesRatchet.Web.Models;

public sealed class SettingsModel
{
    public string TimeZoneId { get; set; } = string.Empty;

    public string Theme { get; set; } = "light";
}
