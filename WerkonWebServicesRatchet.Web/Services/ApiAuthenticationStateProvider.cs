using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using WerkonWebServicesRatchet.Web.Models;

namespace WerkonWebServicesRatchet.Web.Services;

public sealed class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly RatchetApiClient _apiClient;

    public ApiAuthenticationStateProvider(RatchetApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        CurrentUserModel? user;

        try
        {
            user = await _apiClient.GetCurrentUserAsync();
        }
        catch
        {
            user = null;
        }

        if (user is null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("display_name", user.DisplayName)
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, authenticationType: "Cookies");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
