using System.Collections.Concurrent;
using System.Net;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class CircuitCookieStore
{
    private readonly ConcurrentDictionary<string, CookieContainer> _containers = new(StringComparer.Ordinal);

    public CookieContainer GetOrCreate(string key) =>
        _containers.GetOrAdd(key, _ => new CookieContainer());

    public bool TryGet(string key, out CookieContainer container) =>
        _containers.TryGetValue(key, out container!);

    public void Remove(string key)
    {
        _containers.TryRemove(key, out _);
    }

    public void Migrate(string sourceKey, string targetKey)
    {
        if (string.Equals(sourceKey, targetKey, StringComparison.Ordinal))
        {
            return;
        }

        if (!TryGet(sourceKey, out var sourceContainer))
        {
            return;
        }

        var targetContainer = GetOrCreate(targetKey);

        foreach (Cookie cookie in sourceContainer.GetAllCookies())
        {
            try
            {
                targetContainer.Add(cookie);
            }
            catch (CookieException)
            {
            }
        }

        Remove(sourceKey);
    }
}
