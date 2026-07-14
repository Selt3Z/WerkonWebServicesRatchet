using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Clients;
using WerkonWebServicesRatchet.Contracts.Vehicles;
using WerkonWebServicesRatchet.Contracts.Visits;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Features.Vehicles;

[ApiController]
[Route("api/vehicles")]
[Authorize(Policy = AuthorizationPolicies.BusinessData)]
public sealed class VehiclesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public VehiclesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleResponse>>> Search(
    [FromQuery] string? licensePlate,
    [FromQuery] string? vin,
    CancellationToken cancellationToken)
    {
        var query = _dbContext.Vehicles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(licensePlate))
        {
            var normalizedLicensePlate = licensePlate.Trim().ToLower();
            query = query.Where(x => x.LicensePlate.ToLower().Contains(normalizedLicensePlate));
        }

        if (!string.IsNullOrWhiteSpace(vin))
        {
            var normalizedVin = vin.Trim().ToLower();
            query = query.Where(x => x.Vin != null && x.Vin.ToLower().Contains(normalizedVin));
        }

        var response = await query
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new VehicleResponse
            {
                Id = x.Id,
                ClientId = x.ClientId,
                Brand = x.Brand,
                Model = x.Model,
                Year = x.Year,
                LicensePlate = x.LicensePlate,
                Vin = x.Vin,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("~/api/clients/{clientId:guid}/vehicles")]
    public async Task<ActionResult<List<VehicleResponse>>> GetByClientId(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var clientExists = await _dbContext.Clients
            .AnyAsync(x => x.Id == clientId, cancellationToken);

        if (!clientExists)
        {
            return NotFound();
        }

        var response = await _dbContext.Vehicles
            .Where(x => x.ClientId == clientId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new VehicleResponse
            {
                Id = x.Id,
                ClientId = x.ClientId,
                Brand = x.Brand,
                Model = x.Model,
                Year = x.Year,
                LicensePlate = x.LicensePlate,
                Vin = x.Vin,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _dbContext.Vehicles
            .IgnoreQueryFilters()
            .Where(x => x.Id == id)
            .Select(x => new VehicleResponse
            {
                Id = x.Id,
                ClientId = x.ClientId,
                Brand = x.Brand,
                Model = x.Model,
                Year = x.Year,
                LicensePlate = x.LicensePlate,
                Vin = x.Vin,
                IsArchived = x.IsArchived,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost("~/api/clients/{clientId:guid}/vehicles")]
    public async Task<ActionResult<VehicleResponse>> Create(
        Guid clientId,
        SaveVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var clientExists = await _dbContext.Clients
            .AnyAsync(x => x.Id == clientId, cancellationToken);

        if (!clientExists)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Brand))
        {
            ModelState.AddModelError(nameof(request.Brand), "Brand is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            ModelState.AddModelError(nameof(request.Model), "Model is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LicensePlate))
        {
            ModelState.AddModelError(nameof(request.LicensePlate), "License plate is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            LicensePlate = request.LicensePlate.Trim(),
            Vin = string.IsNullOrWhiteSpace(request.Vin) ? null : request.Vin.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new VehicleResponse
        {
            Id = vehicle.Id,
            ClientId = vehicle.ClientId,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Vin = vehicle.Vin,
            CreatedAtUtc = vehicle.CreatedAtUtc
        };

        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleResponse>> Update(
    Guid id,
    SaveVehicleRequest request,
    CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (vehicle is null)
        {
            return NotFound($"Vehicle with id {id} wasn't found.");
        }

        if (string.IsNullOrWhiteSpace(request.Brand))
        {
            ModelState.AddModelError(nameof(request.Brand), "Brand is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            ModelState.AddModelError(nameof(request.Model), "Model is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LicensePlate))
        {
            ModelState.AddModelError(nameof(request.LicensePlate), "License plate is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        vehicle.Brand = request.Brand.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Year = request.Year;
        vehicle.LicensePlate = request.LicensePlate.Trim();
        vehicle.Vin = string.IsNullOrWhiteSpace(request.Vin)
            ? null
            : request.Vin.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new VehicleResponse
        {
            Id = vehicle.Id,
            ClientId = vehicle.ClientId,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Vin = vehicle.Vin,
            CreatedAtUtc = vehicle.CreatedAtUtc
        };

        return Ok(response);
    }

    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<VehicleDetailsResponse>> GetDetails(
    Guid id,
    CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles
            .IgnoreQueryFilters()
            .Include(x => x.Client)
            .Include(x => x.Visits.Where(v => !v.IsArchived))
            .Include(x => x.Reminders)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (vehicle is null)
        {
            return NotFound();
        }

        var hasDependentRecords = await _dbContext.Visits
            .IgnoreQueryFilters()
            .AnyAsync(x => x.VehicleId == id, cancellationToken)
            || vehicle.Reminders.Count > 0;

        var response = new VehicleDetailsResponse
        {
            Id = vehicle.Id,
            ClientId = vehicle.ClientId,
            ClientFullName = vehicle.Client.FullName,
            ClientPhoneNumber = vehicle.Client.PhoneNumber,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Vin = vehicle.Vin,
            IsArchived = vehicle.IsArchived,
            CreatedAtUtc = vehicle.CreatedAtUtc,
            HasDependentRecords = hasDependentRecords,
            Visits = vehicle.Visits
                .OrderByDescending(x => x.VisitedAtUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Select(x => new VisitResponse
                {
                    Id = x.Id,
                    Number = x.Number,
                    VehicleId = x.VehicleId,
                    VisitedAtUtc = x.VisitedAtUtc,
                    MileageAtVisit = x.MileageAtVisit,
                    CustomerComplaint = x.CustomerComplaint,
                    MechanicComment = x.MechanicComment,
                    Status = x.Status,
                    IsArchived = x.IsArchived,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToList(),
            Reminders = vehicle.Reminders
                .OrderByDescending(x => x.ReminderAtUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Select(x => new VehicleReminderItemResponse
                {
                    Id = x.Id,
                    ReminderAtUtc = x.ReminderAtUtc,
                    Note = x.Note,
                    IsClosed = x.IsClosed
                })
                .ToList()
        };

        return Ok(response);
    }

    [HttpPatch("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) =>
        await SetArchivedAsync(id, archived: true, cancellationToken);

    [HttpPatch("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken) =>
        await SetArchivedAsync(id, archived: false, cancellationToken);

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.HardDeleteRecords)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (vehicle is null)
        {
            return NotFound();
        }

        var hasVisits = await _dbContext.Visits
            .IgnoreQueryFilters()
            .AnyAsync(x => x.VehicleId == id, cancellationToken);

        var hasReminders = await _dbContext.Reminders
            .AnyAsync(x => x.VehicleId == id, cancellationToken);

        if (hasVisits || hasReminders)
        {
            return Conflict(new { message = "Vehicle has visits or reminders and cannot be deleted. Archive it instead." });
        }

        _dbContext.Vehicles.Remove(vehicle);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<IActionResult> SetArchivedAsync(
        Guid id,
        bool archived,
        CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (vehicle is null)
        {
            return NotFound();
        }

        if (vehicle.IsArchived != archived)
        {
            vehicle.IsArchived = archived;
            vehicle.ArchivedAtUtc = archived ? DateTime.UtcNow : null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }
}
