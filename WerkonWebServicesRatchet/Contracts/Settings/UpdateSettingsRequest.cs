namespace WerkonWebServicesRatchet.Contracts.Settings;

public sealed class UpdateSettingsRequest
{
    public string? TimeZoneId { get; set; }

    public string? Theme { get; set; }
}
