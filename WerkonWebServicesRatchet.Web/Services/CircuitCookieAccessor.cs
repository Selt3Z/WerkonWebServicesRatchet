namespace WerkonWebServicesRatchet.Web.Services;

public interface ICircuitCookieAccessor
{
    string ScopeKey { get; }

    string StorageKey { get; }
}

public sealed class CircuitCookieAccessor : ICircuitCookieAccessor
{
    public CircuitCookieAccessor(IHttpContextAccessor httpContextAccessor)
    {
        var sessionId = httpContextAccessor.HttpContext?.Request.Cookies[BrowserSessionCookie.CookieName];

        ScopeKey = string.IsNullOrWhiteSpace(sessionId)
            ? Guid.NewGuid().ToString("N")
            : sessionId;
    }

    public string ScopeKey { get; }

    public string StorageKey => ScopeKey;
}
