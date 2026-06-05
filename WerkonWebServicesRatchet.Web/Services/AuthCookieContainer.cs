using System.Net;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class AuthCookieContainer
{
    private readonly CircuitCookieStore _store;
    private readonly ICircuitCookieAccessor _accessor;
    private readonly CookieContainer _jar = new();

    public AuthCookieContainer(CircuitCookieStore store, ICircuitCookieAccessor accessor)
    {
        _store = store;
        _accessor = accessor;
    }

    public CookieContainer Cookies => _jar;

    public void RestoreFromStore()
    {
        if (!_store.TryGet(_accessor.StorageKey, out var stored))
        {
            return;
        }

        CookieContainerHelper.CopyCookies(stored, _jar);
    }

    public void SaveToStore()
    {
        var stored = _store.GetOrCreate(_accessor.StorageKey);
        CookieContainerHelper.ReplaceCookies(_jar, stored);
    }

    public void Clear()
    {
        CookieContainerHelper.Clear(_jar);
        _store.Remove(_accessor.ScopeKey);

        if (!string.IsNullOrEmpty(_accessor.CircuitId))
        {
            _store.Remove(_accessor.CircuitId);
        }
    }
}

internal static class CookieContainerHelper
{
    public static void CopyCookies(CookieContainer source, CookieContainer target)
    {
        foreach (Cookie cookie in source.GetAllCookies())
        {
            try
            {
                target.Add(cookie);
            }
            catch (CookieException)
            {
            }
        }
    }

    public static void ReplaceCookies(CookieContainer source, CookieContainer target)
    {
        Clear(target);
        CopyCookies(source, target);
    }

    public static void Clear(CookieContainer container)
    {
        foreach (Cookie cookie in container.GetAllCookies())
        {
            cookie.Expired = true;

            try
            {
                container.Add(cookie);
            }
            catch (CookieException)
            {
            }
        }
    }
}
