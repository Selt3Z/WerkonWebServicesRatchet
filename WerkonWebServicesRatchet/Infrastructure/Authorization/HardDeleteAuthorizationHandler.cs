using Microsoft.AspNetCore.Authorization;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Settings;

namespace WerkonWebServicesRatchet.Infrastructure.Authorization;

public sealed class HardDeleteRequirement : IAuthorizationRequirement;

public sealed class HardDeleteAuthorizationHandler : AuthorizationHandler<HardDeleteRequirement>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public HardDeleteAuthorizationHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HardDeleteRequirement requirement)
    {
        if (context.User.IsInRole(AppRoles.Administrator))
        {
            context.Succeed(requirement);
            return;
        }

        if (!context.User.IsInRole(AppRoles.Manager))
        {
            return;
        }

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var appSettingsService = scope.ServiceProvider.GetRequiredService<AppSettingsService>();

        if (await appSettingsService.GetManagerCanHardDeleteAsync())
        {
            context.Succeed(requirement);
        }
    }
}
