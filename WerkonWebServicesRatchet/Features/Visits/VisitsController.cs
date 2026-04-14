using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Visits;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Features.Visits;

[ApiController]
public sealed class VisitsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public VisitsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("api/vehicles/{vehicleId:guid}/visits")]
    public async Task<ActionResult<List<VisitResponse>>> GetByVehicleId(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var vehicleExists = await _dbContext.Vehicles
            .AnyAsync(x => x.Id == vehicleId, cancellationToken);

        if (!vehicleExists)
        {
            return NotFound();
        }

        var response = await _dbContext.Visits
            .Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.VisitedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new VisitResponse
            {
                Id = x.Id,
                VehicleId = x.VehicleId,
                VisitedAtUtc = x.VisitedAtUtc,
                MileageAtVisit = x.MileageAtVisit,
                CustomerComplaint = x.CustomerComplaint,
                MechanicComment = x.MechanicComment,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("api/visits/{id:guid}")]
    public async Task<ActionResult<VisitResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _dbContext.Visits
            .Where(x => x.Id == id)
            .Select(x => new VisitResponse
            {
                Id = x.Id,
                VehicleId = x.VehicleId,
                VisitedAtUtc = x.VisitedAtUtc,
                MileageAtVisit = x.MileageAtVisit,
                CustomerComplaint = x.CustomerComplaint,
                MechanicComment = x.MechanicComment,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost("api/vehicles/{vehicleId:guid}/visits")]
    public async Task<ActionResult<VisitResponse>> Create(
        Guid vehicleId,
        SaveVisitRequest request,
        CancellationToken cancellationToken)
    {
        var vehicleExists = await _dbContext.Vehicles
            .AnyAsync(x => x.Id == vehicleId, cancellationToken);

        if (!vehicleExists)
        {
            return NotFound();
        }

        if (request.VisitedAtUtc == default)
        {
            ModelState.AddModelError(nameof(request.VisitedAtUtc), "VisitedAtUtc is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerComplaint))
        {
            ModelState.AddModelError(nameof(request.CustomerComplaint), "Customer complaint is required.");
        }

        if (request.MileageAtVisit is < 0)
        {
            ModelState.AddModelError(nameof(request.MileageAtVisit), "MileageAtVisit cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            VisitedAtUtc = request.VisitedAtUtc,
            MileageAtVisit = request.MileageAtVisit,
            CustomerComplaint = request.CustomerComplaint.Trim(),
            MechanicComment = string.IsNullOrWhiteSpace(request.MechanicComment)
                ? null
                : request.MechanicComment.Trim(),
            Status = VisitStatus.Created,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Visits.Add(visit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new VisitResponse
        {
            Id = visit.Id,
            VehicleId = visit.VehicleId,
            VisitedAtUtc = visit.VisitedAtUtc,
            MileageAtVisit = visit.MileageAtVisit,
            CustomerComplaint = visit.CustomerComplaint,
            MechanicComment = visit.MechanicComment,
            Status = visit.Status,
            CreatedAtUtc = visit.CreatedAtUtc
        };

        return CreatedAtAction(nameof(GetById), new { id = visit.Id }, response);
    }

    [HttpGet("api/visits/{id:guid}/details")]
    public async Task<ActionResult<VisitDetailsResponse>> GetDetails(
    Guid id,
    CancellationToken cancellationToken) //Берёт один визит, подтягивает его работы, считает итоговую сумму и отдаёт всё разом.
    {
        var visit = await _dbContext.Visits
            .Include(x => x.ServiceItems)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (visit is null)
        {
            return NotFound();
        }

        var serviceItems = visit.ServiceItems
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
            .ToList();

        var response = new VisitDetailsResponse
        {
            Id = visit.Id,
            VehicleId = visit.VehicleId,
            VisitedAtUtc = visit.VisitedAtUtc,
            MileageAtVisit = visit.MileageAtVisit,
            CustomerComplaint = visit.CustomerComplaint,
            MechanicComment = visit.MechanicComment,
            Status = visit.Status,
            CreatedAtUtc = visit.CreatedAtUtc,
            ServiceItems = serviceItems,
            TotalAmount = serviceItems.Sum(x => x.TotalPrice)
        };

        return Ok(response);
    }

    [HttpPatch("api/visits/{id:guid}/status")]
    public async Task<ActionResult<VisitResponse>> UpdateStatus(
    Guid id,
    UpdateVisitStatusRequest request,
    CancellationToken cancellationToken) //Находит визит, меняет статус, сохраняет.
    {
        var visit = await _dbContext.Visits
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (visit is null)
        {
            return NotFound();
        }

        visit.Status = request.Status;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new VisitResponse
        {
            Id = visit.Id,
            VehicleId = visit.VehicleId,
            VisitedAtUtc = visit.VisitedAtUtc,
            MileageAtVisit = visit.MileageAtVisit,
            CustomerComplaint = visit.CustomerComplaint,
            MechanicComment = visit.MechanicComment,
            Status = visit.Status,
            CreatedAtUtc = visit.CreatedAtUtc
        };

        return Ok(response);
    }
}