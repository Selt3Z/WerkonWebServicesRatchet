namespace WerkonWebServicesRatchet.Contracts.Settings;

public sealed class SettingsResponse
{
    public string TimeZoneId { get; set; } = string.Empty;

    public string Theme { get; set; } = "light";
}
