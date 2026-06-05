using System.Globalization;
using Microsoft.AspNetCore.Components;
using WerkonWebServicesRatchet.Web.Localization;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class LocalizationService
{
    private readonly NavigationManager _navigationManager;

    public LocalizationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public string CultureCode =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    public IReadOnlyList<LanguageOption> AvailableLanguages => LanguageOption.All;

    public LanguageOption CurrentLanguage =>
        LanguageOption.All.FirstOrDefault(x => x.Code.Equals(CultureCode, StringComparison.OrdinalIgnoreCase))
        ?? LanguageOption.Russian;

    public string this[string key] => Get(key);

    public string Get(string key) =>
        LocalizedStrings.Get(CultureCode, key);

    public string GetRoleDisplayName(string role) => role switch
    {
        "Administrator" => Get("Role_Administrator"),
        "Manager" => Get("Role_Manager"),
        "Mechanic" => Get("Role_Mechanic"),
        _ => role
    };

    public string GetVisitStatus(int status) => status switch
    {
        1 => Get("Status_Created"),
        2 => Get("Status_InProgress"),
        3 => Get("Status_Completed"),
        4 => Get("Status_Cancelled"),
        _ => $"{Get("Status_Unknown")} ({status})"
    };

    public string GetAuditAction(string action) => action switch
    {
        "Created" => Get("History_Action_Created"),
        "Updated" => Get("History_Action_Updated"),
        "Deleted" => Get("History_Action_Deleted"),
        _ => action
    };

    public string GetAuditEntityType(string entityType) => entityType switch
    {
        "Client" => Get("History_Entity_Client"),
        "Vehicle" => Get("History_Entity_Vehicle"),
        "Visit" => Get("History_Entity_Visit"),
        "ServiceItem" => Get("History_Entity_ServiceItem"),
        "Reminder" => Get("History_Entity_Reminder"),
        "CatalogService" => Get("History_Entity_CatalogService"),
        _ => entityType
    };

    public string GetCultureSwitchUrl(string cultureCode)
    {
        var returnUrl = GetSafeReturnUrl();
        return $"/culture/set/{cultureCode}?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    public static string? NormalizeCultureName(string? culture)
    {
        return culture?.ToLowerInvariant() switch
        {
            "ru" or "ru-ru" => "ru-RU",
            "en" or "en-us" => "en-US",
            "ja" or "ja-jp" => "ja-JP",
            _ => null
        };
    }

    public static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith('/')
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
            || returnUrl.StartsWith("/culture/", StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }

        return returnUrl;
    }

    private string GetSafeReturnUrl()
    {
        var relativePath = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return "/";
        }

        return GetSafeReturnUrl("/" + relativePath);
    }
}
