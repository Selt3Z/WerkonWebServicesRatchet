namespace WerkonWebServicesRatchet.Contracts.Settings;

public sealed class UpdateSettingsRequest
{
    public string? TimeZoneId { get; set; }

    public string? Theme { get; set; }

    public string? CurrencyCode { get; set; }

    public string? DefaultVisitStatus { get; set; }

    public int? ReminderLookbackDays { get; set; }

    public bool? HideArchivedByDefault { get; set; }

    public bool? ManagerCanHardDelete { get; set; }

    public bool? MechanicCanAddCatalogServices { get; set; }

    public int? AuditRetentionDays { get; set; }

    public int? ListPageSize { get; set; }
}
