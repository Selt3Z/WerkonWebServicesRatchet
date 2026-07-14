using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Features.Dev;

[ApiController]
[Route("api/dev")]
[Authorize(Policy = AuthorizationPolicies.ManageUsers)]
public sealed class DevController : ControllerBase
{
    public const string DemoClientNoteMarker = "pagination-demo";

    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public DevController(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    [HttpPost("seed-clients")]
    public async Task<ActionResult<object>> SeedDemoClients(
        [FromQuery] int count = 50,
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        count = Math.Clamp(count, 1, 500);

        var existing = await _dbContext.Clients
            .IgnoreQueryFilters()
            .CountAsync(x => x.Notes == DemoClientNoteMarker, cancellationToken);

        if (existing >= count)
        {
            return Ok(new { created = 0, total = existing, message = "Demo clients already exist." });
        }

        var toCreate = count - existing;
        var startIndex = existing + 1;
        var now = DateTime.UtcNow;
        var clients = new List<Client>(toCreate);

        for (var i = 0; i < toCreate; i++)
        {
            var index = startIndex + i;
            clients.Add(new Client
            {
                Id = Guid.NewGuid(),
                FullName = $"Демо клиент {index:D3}",
                PhoneNumber = $"+7900{index:D7}",
                Notes = DemoClientNoteMarker,
                CreatedAtUtc = now.AddMinutes(-index)
            });
        }

        _dbContext.Clients.AddRange(clients);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { created = toCreate, total = existing + toCreate });
    }
}
