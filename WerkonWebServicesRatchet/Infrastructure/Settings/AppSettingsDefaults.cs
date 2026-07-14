namespace WerkonWebServicesRatchet.Infrastructure.Settings;

public static class AppSettingsDefaults
{
    public const string CurrencyCode = "RUB";

    public const int CurrencyDecimalPlaces = 2;

    public const string DefaultVisitStatus = "Created";

    public const int ReminderLookbackDays = 7;

    public const bool HideArchivedByDefault = true;

    public const bool ManagerCanHardDelete = false;

    public const bool MechanicCanAddCatalogServices = false;

    public const int AuditRetentionDays = 30;

    public const int ListPageSize = 30;
}
