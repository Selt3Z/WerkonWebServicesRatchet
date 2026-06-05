using Microsoft.JSInterop;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class BrowserSessionCookie
{
    public const string CookieName = "Ratchet.Web.Session";

    private readonly IJSRuntime _jsRuntime;

    public BrowserSessionCookie(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask PersistAsync(string sessionId) =>
        _jsRuntime.InvokeVoidAsync("ratchetAuth.setSession", sessionId);

    public ValueTask ClearAsync() =>
        _jsRuntime.InvokeVoidAsync("ratchetAuth.clearSession");
}
