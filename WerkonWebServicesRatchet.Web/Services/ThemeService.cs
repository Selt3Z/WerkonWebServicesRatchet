using Microsoft.JSInterop;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class ThemeService
{
    private readonly IJSRuntime _jsRuntime;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string Current { get; private set; } = "light";

    public async Task ApplyAsync(string? theme)
    {
        Current = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
        await _jsRuntime.InvokeVoidAsync("ratchetTheme.set", Current);
    }
}
