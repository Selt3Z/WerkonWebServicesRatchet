using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class AuthCircuitHandler : CircuitHandler
{
    private readonly ICircuitCookieAccessor _cookieAccessor;
    private readonly CircuitCookieStore _cookieStore;
    private readonly AuthCookieContainer _cookieContainer;

    public AuthCircuitHandler(
        ICircuitCookieAccessor cookieAccessor,
        CircuitCookieStore cookieStore,
        AuthCookieContainer cookieContainer)
    {
        _cookieAccessor = cookieAccessor;
        _cookieStore = cookieStore;
        _cookieContainer = cookieContainer;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _cookieStore.Migrate(_cookieAccessor.ScopeKey, circuit.Id);
        _cookieAccessor.CircuitId = circuit.Id;
        _cookieContainer.RestoreFromStore();
        return Task.CompletedTask;
    }
}
