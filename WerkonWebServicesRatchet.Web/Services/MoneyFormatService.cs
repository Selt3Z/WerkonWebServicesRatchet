using System.Globalization;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class MoneyFormatService
{
    private readonly AppPreferencesService _preferences;

    public MoneyFormatService(AppPreferencesService preferences)
    {
        _preferences = preferences;
    }

    public string Format(decimal amount)
    {
        var culture = GetCulture(_preferences.CurrencyCode);
        var formatted = amount.ToString($"N{_preferences.CurrencyDecimalPlaces}", culture);

        return _preferences.CurrencyCode switch
        {
            "RUB" => $"{formatted} ₽",
            "USD" => $"${formatted}",
            "EUR" => $"€{formatted}",
            _ => $"{formatted} {_preferences.CurrencyCode}"
        };
    }

    private static CultureInfo GetCulture(string currencyCode) => currencyCode switch
    {
        "USD" => CultureInfo.GetCultureInfo("en-US"),
        "EUR" => CultureInfo.GetCultureInfo("de-DE"),
        _ => CultureInfo.GetCultureInfo("ru-RU")
    };
}
