using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Settings;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Pdf;
using WerkonWebServicesRatchet.Infrastructure.Persistence;
using WerkonWebServicesRatchet.Infrastructure.Time;

namespace WerkonWebServicesRatchet.Infrastructure.Settings;

public sealed class AppSettingsService
{
    private const int MaxLogoBytes = 512 * 1024;

    private static readonly HashSet<string> AllowedLogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AppDbContext _dbContext;
    private readonly AppTimeZone _appTimeZone;
    private readonly IConfiguration _configuration;

    public AppSettingsService(
        AppDbContext dbContext,
        AppTimeZone appTimeZone,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _appTimeZone = appTimeZone;
        _configuration = configuration;
    }

    public async Task<SettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return new SettingsResponse
        {
            TimeZoneId = await GetTimeZoneIdAsync(cancellationToken),
            Theme = await GetThemeAsync(cancellationToken)
        };
    }

    public async Task<SettingsResponse> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.TimeZoneId))
        {
            await SetTimeZoneIdAsync(request.TimeZoneId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.Theme))
        {
            await SetThemeAsync(request.Theme, cancellationToken);
        }

        return await GetSettingsAsync(cancellationToken);
    }

    public async Task<string> GetTimeZoneIdAsync(CancellationToken cancellationToken = default)
    {
        var storedValue = await GetSettingValueAsync(AppSettingKeys.TimeZone, cancellationToken);

        if (!string.IsNullOrWhiteSpace(storedValue))
        {
            return storedValue;
        }

        return _configuration[AppSettingKeys.TimeZone] ?? "Europe/Moscow";
    }

    public async Task<string> GetThemeAsync(CancellationToken cancellationToken = default)
    {
        var storedValue = await GetSettingValueAsync(AppSettingKeys.Theme, cancellationToken);

        if (!string.IsNullOrWhiteSpace(storedValue))
        {
            return NormalizeTheme(storedValue);
        }

        return NormalizeTheme(_configuration[AppSettingKeys.Theme]);
    }

    public async Task<string> SetTimeZoneIdAsync(string timeZoneId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)
            || !TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out _))
        {
            throw new ArgumentException("Invalid time zone id.", nameof(timeZoneId));
        }

        var normalizedId = timeZoneId.Trim();
        await SaveSettingAsync(AppSettingKeys.TimeZone, normalizedId, cancellationToken);
        _appTimeZone.SetTimeZoneId(normalizedId);

        return normalizedId;
    }

    public async Task<string> SetThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        var normalizedTheme = NormalizeTheme(theme);
        await SaveSettingAsync(AppSettingKeys.Theme, normalizedTheme, cancellationToken);
        return normalizedTheme;
    }

    public async Task<OrganizationProfileResponse> GetOrganizationProfileAsync(
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadOrganizationProfileAsync(cancellationToken);
        var logo = await LoadOrganizationLogoAsync(cancellationToken);

        return MapOrganizationProfileResponse(profile, logo);
    }

    public async Task<OrganizationProfileResponse> UpdateOrganizationProfileAsync(
        UpdateOrganizationProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = new OrganizationProfile
        {
            OrganizationName = request.OrganizationName.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim(),
            Address = request.Address.Trim(),
            Inn = request.Inn.Trim(),
            Kpp = request.Kpp.Trim(),
            Ogrn = request.Ogrn.Trim(),
            BankAccount = request.BankAccount.Trim(),
            Bik = request.Bik.Trim()
        };

        await SaveSettingAsync(
            AppSettingKeys.OrganizationProfile,
            JsonSerializer.Serialize(profile, JsonOptions),
            cancellationToken);

        var logo = await LoadOrganizationLogoAsync(cancellationToken);
        return MapOrganizationProfileResponse(profile, logo);
    }

    public async Task<OrganizationProfileResponse> UploadOrganizationLogoAsync(
        UploadOrganizationLogoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ContentType)
            || !AllowedLogoContentTypes.Contains(request.ContentType.Trim()))
        {
            throw new ArgumentException("Unsupported logo content type.");
        }

        if (string.IsNullOrWhiteSpace(request.DataBase64))
        {
            throw new ArgumentException("Logo data is required.");
        }

        byte[] logoBytes;

        try
        {
            logoBytes = Convert.FromBase64String(request.DataBase64.Trim());
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid logo data.");
        }

        if (logoBytes.Length == 0 || logoBytes.Length > MaxLogoBytes)
        {
            throw new ArgumentException($"Logo size must be between 1 byte and {MaxLogoBytes} bytes.");
        }

        var logo = new OrganizationLogoData
        {
            ContentType = request.ContentType.Trim(),
            DataBase64 = request.DataBase64.Trim()
        };

        await SaveSettingAsync(
            AppSettingKeys.OrganizationLogo,
            JsonSerializer.Serialize(logo, JsonOptions),
            cancellationToken);

        var profile = await LoadOrganizationProfileAsync(cancellationToken);
        return MapOrganizationProfileResponse(profile, logo);
    }

    public async Task<OrganizationProfileResponse> DeleteOrganizationLogoAsync(
        CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.Key == AppSettingKeys.OrganizationLogo, cancellationToken);

        if (setting is not null)
        {
            _dbContext.AppSettings.Remove(setting);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var profile = await LoadOrganizationProfileAsync(cancellationToken);
        return MapOrganizationProfileResponse(profile, logo: null);
    }

    public async Task<OrganizationDocumentInfo> GetOrganizationDocumentInfoAsync(
        CancellationToken cancellationToken = default)
    {
        var profile = await LoadOrganizationProfileAsync(cancellationToken);
        var logo = await LoadOrganizationLogoAsync(cancellationToken);

        byte[]? logoBytes = null;

        if (logo is not null)
        {
            try
            {
                logoBytes = Convert.FromBase64String(logo.DataBase64);
            }
            catch (FormatException)
            {
                logoBytes = null;
            }
        }

        return new OrganizationDocumentInfo
        {
            OrganizationName = profile.OrganizationName,
            Phone = profile.Phone,
            Email = profile.Email,
            Address = profile.Address,
            Inn = profile.Inn,
            Kpp = profile.Kpp,
            Ogrn = profile.Ogrn,
            BankAccount = profile.BankAccount,
            Bik = profile.Bik,
            LogoBytes = logoBytes
        };
    }

    private async Task<OrganizationProfile> LoadOrganizationProfileAsync(CancellationToken cancellationToken)
    {
        var storedValue = await GetSettingValueAsync(AppSettingKeys.OrganizationProfile, cancellationToken);

        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return new OrganizationProfile();
        }

        return JsonSerializer.Deserialize<OrganizationProfile>(storedValue, JsonOptions)
            ?? new OrganizationProfile();
    }

    private async Task<OrganizationLogoData?> LoadOrganizationLogoAsync(CancellationToken cancellationToken)
    {
        var storedValue = await GetSettingValueAsync(AppSettingKeys.OrganizationLogo, cancellationToken);

        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return null;
        }

        return JsonSerializer.Deserialize<OrganizationLogoData>(storedValue, JsonOptions);
    }

    private static OrganizationProfileResponse MapOrganizationProfileResponse(
        OrganizationProfile profile,
        OrganizationLogoData? logo)
    {
        string? logoDataUrl = null;

        if (logo is not null
            && !string.IsNullOrWhiteSpace(logo.ContentType)
            && !string.IsNullOrWhiteSpace(logo.DataBase64))
        {
            logoDataUrl = $"data:{logo.ContentType};base64,{logo.DataBase64}";
        }

        return new OrganizationProfileResponse
        {
            OrganizationName = profile.OrganizationName,
            Phone = profile.Phone,
            Email = profile.Email,
            Address = profile.Address,
            Inn = profile.Inn,
            Kpp = profile.Kpp,
            Ogrn = profile.Ogrn,
            BankAccount = profile.BankAccount,
            Bik = profile.Bik,
            HasLogo = logoDataUrl is not null,
            LogoDataUrl = logoDataUrl
        };
    }

    private async Task<string?> GetSettingValueAsync(string key, CancellationToken cancellationToken)
    {
        return await _dbContext.AppSettings
            .AsNoTracking()
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task SaveSettingAsync(string key, string value, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (setting is null)
        {
            setting = new AppSetting
            {
                Key = key,
                Value = value
            };
            _dbContext.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeTheme(string? theme) =>
        string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
}
