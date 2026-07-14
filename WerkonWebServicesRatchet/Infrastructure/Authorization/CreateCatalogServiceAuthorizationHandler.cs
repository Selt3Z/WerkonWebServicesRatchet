using Microsoft.AspNetCore.Authorization;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Settings;

namespace WerkonWebServicesRatchet.Infrastructure.Authorization;

public sealed class CreateCatalogServiceRequirement : IAuthorizationRequirement;

public sealed class CreateCatalogServiceAuthorizationHandler : AuthorizationHandler<CreateCatalogServiceRequirement>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CreateCatalogServiceAuthorizationHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CreateCatalogServiceRequirement requirement)
    {
        if (context.User.IsInRole(AppRoles.Administrator)
            || context.User.IsInRole(AppRoles.Manager))
        {
            context.Succeed(requirement);
            return;
        }

        if (!context.User.IsInRole(AppRoles.Mechanic))
        {
            return;
        }

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var appSettingsService = scope.ServiceProvider.GetRequiredService<AppSettingsService>();

        if (await appSettingsService.GetMechanicCanAddCatalogServicesAsync())
        {
            context.Succeed(requirement);
        }
    }
}
