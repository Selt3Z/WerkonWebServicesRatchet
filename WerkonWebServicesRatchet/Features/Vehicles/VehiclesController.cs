using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Vehicles;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Features.Vehicles;

[ApiController]
public sealed class VehiclesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public VehiclesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("api/clients/{clientId:guid}/vehicles")]
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

    [HttpGet("api/vehicles/{id:guid}")]
    public async Task<ActionResult<VehicleResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _dbContext.Vehicles
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
                CreatedAtUtc = x.CreatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost("api/clients/{clientId:guid}/vehicles")]
    public async Task<ActionResult<VehicleResponse>> Create(
        Guid clientId,
        CreateVehicleRequest request,
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
}
