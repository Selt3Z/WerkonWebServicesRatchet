using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Search;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Features.Search;

[ApiController]
[Route("api/search")]
[Authorize(Policy = AuthorizationPolicies.BusinessData)]
public sealed class SearchController : ControllerBase
{
    private const int MaxHits = 8;

    private readonly AppDbContext _dbContext;

    public SearchController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<GlobalSearchResponse>> Search(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return Ok(new GlobalSearchResponse());
        }

        var term = q.Trim().ToLower();

        var clients = await _dbContext.Clients
            .Where(x =>
                x.FullName.ToLower().Contains(term)
                || x.PhoneNumber.ToLower().Contains(term)
                || (x.Notes != null && x.Notes.ToLower().Contains(term)))
            .OrderBy(x => x.FullName)
            .Take(MaxHits)
            .Select(x => new GlobalSearchHit
            {
                Id = x.Id,
                Title = x.FullName,
                Subtitle = x.PhoneNumber,
                Kind = "client"
            })
            .ToListAsync(cancellationToken);

        var vehicles = await _dbContext.Vehicles
            .Where(x =>
                x.LicensePlate.ToLower().Contains(term)
                || (x.Vin != null && x.Vin.ToLower().Contains(term))
                || x.Brand.ToLower().Contains(term)
                || x.Model.ToLower().Contains(term)
                || x.Client.FullName.ToLower().Contains(term)
                || x.Client.PhoneNumber.ToLower().Contains(term))
            .OrderBy(x => x.LicensePlate)
            .Take(MaxHits)
            .Select(x => new GlobalSearchHit
            {
                Id = x.Id,
                Title = x.LicensePlate + " · " + x.Brand + " " + x.Model,
                Subtitle = (x.Vin ?? "—") + " · " + x.Client.FullName,
                Kind = "vehicle"
            })
            .ToListAsync(cancellationToken);

        return Ok(new GlobalSearchResponse
        {
            Clients = clients,
            Vehicles = vehicles
        });
    }
}
