using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Audit;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Features.Audit;

[ApiController]
[Route("api/audit-log")]
[Authorize(Policy = AuthorizationPolicies.ManageUsers)]
public sealed class AuditLogController : ControllerBase
{
    private const int DefaultTake = 200;
    private const int MaxTake = 500;

    private readonly AppDbContext _dbContext;

    public AuditLogController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditLogItemResponse>>> Get(
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(take ?? DefaultTake, 1, MaxTake);

        var response = await _dbContext.AuditLogEntries
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(pageSize)
            .Select(x => new AuditLogItemResponse
            {
                Id = x.Id,
                OccurredAtUtc = x.OccurredAtUtc,
                UserDisplayName = x.UserDisplayName,
                Action = x.Action,
                EntityType = x.EntityType,
                Summary = x.Summary,
                EntityUrl = x.EntityUrl
            })
            .ToListAsync(cancellationToken);

        return Ok(response);
    }
}
