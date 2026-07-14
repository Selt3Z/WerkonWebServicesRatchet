using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.CatalogServices;
using WerkonWebServicesRatchet.Contracts.Common;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Features.CatalogServices;

[ApiController]
[Route("api/catalog-services")]
[Authorize(Policy = AuthorizationPolicies.BusinessData)]
public sealed class CatalogServicesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CatalogServicesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CatalogServiceResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CatalogServices.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(normalizedSearch)
                || (x.Code != null && x.Code.ToLower().Contains(normalizedSearch))
                || (x.Category != null && x.Category.ToLower().Contains(normalizedSearch)));
        }

        var (normalizedSkip, normalizedTake) = QueryPagingExtensions.NormalizePaging(skip, take);

        var response = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => MapResponse(x))
            .ToPagedResponseAsync(normalizedSkip, normalizedTake, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CatalogServiceResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _dbContext.CatalogServices
            .Where(x => x.Id == id)
            .Select(x => MapResponse(x))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CreateCatalogService)]
    public async Task<ActionResult<CatalogServiceResponse>> Create(
        SaveCatalogServiceRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);

        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var entity = new CatalogService
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = NormalizeOptional(request.Code),
            Category = NormalizeOptional(request.Category),
            DefaultUnitPrice = request.DefaultUnitPrice,
            DefaultUnit = request.DefaultUnit.Trim(),
            Description = NormalizeOptional(request.Description),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.CatalogServices.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapResponse(entity));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageServiceCatalog)]
    public async Task<ActionResult<CatalogServiceResponse>> Update(
        Guid id,
        SaveCatalogServiceRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);

        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var entity = await _dbContext.CatalogServices
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        entity.Name = request.Name.Trim();
        entity.Code = NormalizeOptional(request.Code);
        entity.Category = NormalizeOptional(request.Category);
        entity.DefaultUnitPrice = request.DefaultUnitPrice;
        entity.DefaultUnit = request.DefaultUnit.Trim();
        entity.Description = NormalizeOptional(request.Description);
        entity.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapResponse(entity));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageServiceCatalog)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.CatalogServices
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        _dbContext.CatalogServices.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string? ValidateRequest(SaveCatalogServiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Service name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.DefaultUnit))
        {
            return "Default unit is required.";
        }

        if (request.DefaultUnitPrice < 0)
        {
            return "Default unit price cannot be negative.";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static CatalogServiceResponse MapResponse(CatalogService entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            Category = entity.Category,
            DefaultUnitPrice = entity.DefaultUnitPrice,
            DefaultUnit = entity.DefaultUnit,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc
        };
}
