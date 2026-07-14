using Microsoft.JSInterop;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class ThemeService
{
    private readonly IJSRuntime _jsRuntime;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string Current { get; private set; } = AppThemeNames.Light;

    public async Task ApplyAsync(string? theme)
    {
        Current = AppThemeNames.Normalize(theme);
        await _jsRuntime.InvokeVoidAsync("ratchetTheme.set", Current);
    }
}
