using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Persistence;
using WerkonWebServicesRatchet.Infrastructure.Settings;
using WerkonWebServicesRatchet.Infrastructure.Time;

namespace WerkonWebServicesRatchet.Infrastructure.Identity;

public sealed class IdentitySeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentitySeedHostedService> _logger;

    public IdentitySeedHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<IdentitySeedHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        var adminUserName = _configuration["Seed:AdminUserName"] ?? "admin";
        var adminPassword = _configuration["Seed:AdminPassword"] ?? "Admin123!";
        var adminDisplayName = _configuration["Seed:AdminDisplayName"] ?? "Administrator";

        var adminUser = await userManager.FindByNameAsync(adminUserName);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminUserName,
                DisplayName = adminDisplayName,
                Email = $"{adminUserName}@local",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);

            if (!createResult.Succeeded)
            {
                _logger.LogWarning(
                    "Failed to create seed admin user {UserName}: {Errors}",
                    adminUserName,
                    string.Join(", ", createResult.Errors.Select(x => x.Description)));
                return;
            }

            _logger.LogInformation("Seed admin user {UserName} was created.", adminUserName);
        }

        if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Administrator))
        {
            await userManager.AddToRoleAsync(adminUser, AppRoles.Administrator);
        }

        var defaultTimeZoneId = _configuration[AppSettingKeys.TimeZone] ?? "Europe/Moscow";
        var timeZoneSetting = await dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.Key == AppSettingKeys.TimeZone, cancellationToken);

        if (timeZoneSetting is null)
        {
            dbContext.AppSettings.Add(new AppSetting
            {
                Key = AppSettingKeys.TimeZone,
                Value = defaultTimeZoneId
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            timeZoneSetting = await dbContext.AppSettings
                .FirstAsync(x => x.Key == AppSettingKeys.TimeZone, cancellationToken);
        }

        var defaultTheme = _configuration[AppSettingKeys.Theme] ?? "light";
        var themeSetting = await dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.Key == AppSettingKeys.Theme, cancellationToken);

        if (themeSetting is null)
        {
            dbContext.AppSettings.Add(new AppSetting
            {
                Key = AppSettingKeys.Theme,
                Value = defaultTheme
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var appTimeZone = scope.ServiceProvider.GetRequiredService<AppTimeZone>();
        appTimeZone.SetTimeZoneId(timeZoneSetting.Value);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
