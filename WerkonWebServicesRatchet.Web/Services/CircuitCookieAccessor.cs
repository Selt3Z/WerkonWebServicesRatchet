namespace WerkonWebServicesRatchet.Web.Services;

public interface ICircuitCookieAccessor
{
    string ScopeKey { get; }

    string? CircuitId { get; set; }

    string StorageKey { get; }
}

public sealed class CircuitCookieAccessor : ICircuitCookieAccessor
{
    public string ScopeKey { get; } = Guid.NewGuid().ToString("N");

    public string? CircuitId { get; set; }

    public string StorageKey => CircuitId ?? ScopeKey;
}
