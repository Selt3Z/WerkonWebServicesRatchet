using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Persistence;
using WerkonWebServicesRatchet.Infrastructure.Time;

namespace WerkonWebServicesRatchet.Tests;

internal static class TestHelpers
{
    public static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ratchet-tests-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    public static AppTimeZone CreateAppTimeZone(string timeZoneId = "Europe/Moscow")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppTimeZone"] = timeZoneId
            })
            .Build();

        return new AppTimeZone(configuration, NullLogger<AppTimeZone>.Instance);
    }

    public static Client CreateClient(string fullName = "Test Client", string phone = "+70000000000") => new()
    {
        Id = Guid.NewGuid(),
        FullName = fullName,
        PhoneNumber = phone,
        CreatedAtUtc = DateTime.UtcNow
    };

    public static Vehicle CreateVehicle(Guid clientId) => new()
    {
        Id = Guid.NewGuid(),
        ClientId = clientId,
        Brand = "Toyota",
        Model = "Corolla",
        LicensePlate = "A123BC",
        CreatedAtUtc = DateTime.UtcNow
    };

    public static Visit CreateVisit(Guid vehicleId, DateTime? visitedAtUtc = null) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        VisitedAtUtc = visitedAtUtc ?? DateTime.UtcNow,
        CustomerComplaint = "Engine noise",
        Status = VisitStatus.Created,
        CreatedAtUtc = DateTime.UtcNow
    };
}
