using System.Net;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class ApiAuthResponseHandler : DelegatingHandler
{
    private readonly ApiSessionCoordinator _sessionCoordinator;
    private readonly AuthCookieContainer _cookieContainer;

    public ApiAuthResponseHandler(
        ApiSessionCoordinator sessionCoordinator,
        AuthCookieContainer cookieContainer)
    {
        _sessionCoordinator = sessionCoordinator;
        _cookieContainer = cookieContainer;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return SendAsyncCore(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsyncCore(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _cookieContainer.RestoreFromStore();
        var response = await base.SendAsync(request, cancellationToken);
        _cookieContainer.SaveToStore();

        if (response.StatusCode == HttpStatusCode.Unauthorized
            && !IsAnonymousAuthRequest(request.RequestUri))
        {
            _sessionCoordinator.HandleSessionExpired();
        }

        return response;
    }

    private static bool IsAnonymousAuthRequest(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return false;
        }

        var path = requestUri.AbsolutePath;

        return path.EndsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/api/auth/me", StringComparison.OrdinalIgnoreCase);
    }
}
