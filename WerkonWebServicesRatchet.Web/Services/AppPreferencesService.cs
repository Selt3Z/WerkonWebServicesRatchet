using WerkonWebServicesRatchet.Web.Models;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class AppPreferencesService
{
    private bool _loaded;

    public int ListPageSize { get; private set; } = 30;

    public bool HideArchivedByDefault { get; private set; } = true;

    public string CurrencyCode { get; private set; } = "RUB";

    public int CurrencyDecimalPlaces { get; private set; } = 2;

    public int ReminderLookbackDays { get; private set; } = 7;

    public bool ManagerCanHardDelete { get; private set; }

    public bool MechanicCanAddCatalogServices { get; private set; }

    public async Task EnsureLoadedAsync(RatchetApiClient apiClient, CancellationToken cancellationToken = default)
    {
        if (_loaded)
        {
            return;
        }

        var settings = await apiClient.GetSettingsAsync(cancellationToken);

        if (settings is not null)
        {
            Apply(settings);
        }

        _loaded = true;
    }

    public void Apply(SettingsModel settings)
    {
        ListPageSize = settings.ListPageSize > 0 ? settings.ListPageSize : 30;
        HideArchivedByDefault = settings.HideArchivedByDefault;
        CurrencyCode = string.IsNullOrWhiteSpace(settings.CurrencyCode) ? "RUB" : settings.CurrencyCode;
        CurrencyDecimalPlaces = settings.CurrencyDecimalPlaces > 0 ? settings.CurrencyDecimalPlaces : 2;
        ReminderLookbackDays = settings.ReminderLookbackDays > 0 ? settings.ReminderLookbackDays : 7;
        ManagerCanHardDelete = settings.ManagerCanHardDelete;
        MechanicCanAddCatalogServices = settings.MechanicCanAddCatalogServices;
        _loaded = true;
    }
}
