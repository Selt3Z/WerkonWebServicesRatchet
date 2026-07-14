using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Tests.Integration;

/// <summary>
/// Boots the real API pipeline with the database swapped to EF InMemory
/// and the migration/seed hosted service removed.
/// </summary>
public sealed class RatchetApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptors = services
                .Where(x => x.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || x.ServiceType == typeof(DbContextOptions))
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            var seedService = services.SingleOrDefault(x =>
                x.ServiceType == typeof(IHostedService)
                && x.ImplementationType == typeof(IdentitySeedHostedService));

            if (seedService is not null)
            {
                services.Remove(seedService);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"ratchet-integration-{Guid.NewGuid():N}"));
        });
    }
}
