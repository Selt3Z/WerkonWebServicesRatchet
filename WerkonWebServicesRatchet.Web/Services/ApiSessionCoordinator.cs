using Microsoft.AspNetCore.Components;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class ApiSessionCoordinator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthCookieContainer _cookieContainer;
    private readonly NavigationManager _navigationManager;
    private bool _sessionExpiredHandled;

    public ApiSessionCoordinator(
        IServiceProvider serviceProvider,
        AuthCookieContainer cookieContainer,
        NavigationManager navigationManager)
    {
        _serviceProvider = serviceProvider;
        _cookieContainer = cookieContainer;
        _navigationManager = navigationManager;
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
        _navigationManager.NavigateTo("/login", forceLoad: true);
    }
}
