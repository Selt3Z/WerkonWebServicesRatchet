using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class AuthCircuitHandler : CircuitHandler
{
    private readonly AuthCookieContainer _cookieContainer;

    public AuthCircuitHandler(AuthCookieContainer cookieContainer)
    {
        _cookieContainer = cookieContainer;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _cookieContainer.RestoreFromStore();
        return Task.CompletedTask;
    }
}
