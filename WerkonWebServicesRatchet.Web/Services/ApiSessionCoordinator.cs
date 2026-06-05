namespace WerkonWebServicesRatchet.Web.Services;

public sealed class ApiSessionCoordinator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthCookieContainer _cookieContainer;
    private bool _sessionExpiredHandled;

    public ApiSessionCoordinator(
        IServiceProvider serviceProvider,
        AuthCookieContainer cookieContainer)
    {
        _serviceProvider = serviceProvider;
        _cookieContainer = cookieContainer;
    }

    public void HandleSessionExpired()
    {
        if (_sessionExpiredHandled)
        {
            return;
        }

        _sessionExpiredHandled = true;
        _cookieContainer.Clear();
        _serviceProvider.GetRequiredService<ApiAuthenticationStateProvider>().NotifyUserChanged();
    }

    public void MarkSessionActive()
    {
        _sessionExpiredHandled = false;
    }
}
