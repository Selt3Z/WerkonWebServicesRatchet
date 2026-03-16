using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Visits;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Features.Visits;

[ApiController]
public sealed class VisitServiceItemsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public VisitServiceItemsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("api/visits/{visitId:guid}/service-items")]
    public async Task<ActionResult<List<VisitServiceItemResponse>>> GetByVisitId(
        Guid visitId,
        CancellationToken cancellationToken)
    {
        var visitExists = await _dbContext.Visits
            .AnyAsync(x => x.Id == visitId, cancellationToken);

        if (!visitExists)
        {
            return NotFound();
        }

        var response = await _dbContext.VisitServiceItems
            .Where(x => x.VisitId == visitId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new VisitServiceItemResponse
            {
                Id = x.Id,
                VisitId = x.VisitId,
                Name = x.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                TotalPrice = x.Quantity * x.UnitPrice,
                Comment = x.Comment,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPost("api/visits/{visitId:guid}/service-items")]
    public async Task<ActionResult<VisitServiceItemResponse>> Create(
        Guid visitId,
        CreateVisitServiceItemRequest request,
        CancellationToken cancellationToken)
    {
        var visitExists = await _dbContext.Visits
            .AnyAsync(x => x.Id == visitId, cancellationToken);

        if (!visitExists)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name is required.");
        }

        if (request.Quantity <= 0)
        {
            ModelState.AddModelError(nameof(request.Quantity), "Quantity must be greater than zero.");
        }

        if (request.UnitPrice < 0)
        {
            ModelState.AddModelError(nameof(request.UnitPrice), "UnitPrice cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var item = new VisitServiceItem
        {
            Id = Guid.NewGuid(),
            VisitId = visitId,
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.VisitServiceItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new VisitServiceItemResponse
        {
            Id = item.Id,
            VisitId = item.VisitId,
            Name = item.Name,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.Quantity * item.UnitPrice,
            Comment = item.Comment,
            CreatedAtUtc = item.CreatedAtUtc
        };

        return Ok(response);
    }
}