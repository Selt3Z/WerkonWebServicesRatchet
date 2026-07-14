using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Audit;
using WerkonWebServicesRatchet.Contracts.Common;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Features.Audit;

[ApiController]
[Route("api/audit-log")]
[Authorize(Policy = AuthorizationPolicies.ViewAuditLog)]
public sealed class AuditLogController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AuditLogController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditLogItemResponse>>> Get(
        [FromQuery] int? skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var (normalizedSkip, normalizedTake) = QueryPagingExtensions.NormalizePaging(skip, take);

        var response = await _dbContext.AuditLogEntries
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Id)
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
            .ToPagedResponseAsync(normalizedSkip, normalizedTake, cancellationToken);

        return Ok(response);
    }
}
