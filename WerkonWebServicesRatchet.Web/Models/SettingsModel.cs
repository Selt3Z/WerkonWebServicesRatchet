namespace WerkonWebServicesRatchet.Web.Models;

public sealed class SettingsModel
{
    public string TimeZoneId { get; set; } = string.Empty;

    public string Theme { get; set; } = "light";

    public string CurrencyCode { get; set; } = "RUB";

    public int CurrencyDecimalPlaces { get; set; } = 2;

    public string DefaultVisitStatus { get; set; } = "Created";

    public int ReminderLookbackDays { get; set; } = 7;

    public bool HideArchivedByDefault { get; set; } = true;

    public bool ManagerCanHardDelete { get; set; }

    public bool MechanicCanAddCatalogServices { get; set; }

    public int AuditRetentionDays { get; set; } = 30;

    public int ListPageSize { get; set; } = 30;
}
