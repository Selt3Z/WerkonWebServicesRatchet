using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Clients;
using WerkonWebServicesRatchet.Contracts.Vehicles;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Persistence;

namespace WerkonWebServicesRatchet.Features.Clients;

[ApiController]
[Route("api/[controller]")]
public sealed class ClientsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ClientsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClientResponse>>> GetAll(
        [FromQuery] string? name,
        [FromQuery] string? phone,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Clients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalizedName = name.Trim().ToLower();
            query = query.Where(x => x.FullName.ToLower().Contains(normalizedName));
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var normalizedPhone = phone.Trim().ToLower();
            query = query.Where(x => x.PhoneNumber.ToLower().Contains(normalizedPhone));
        }

        var response = await query
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new ClientResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                PhoneNumber = x.PhoneNumber,
                Notes = x.Notes,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _dbContext.Clients
            .Where(x => x.Id == id)
            .Select(x => new ClientResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                PhoneNumber = x.PhoneNumber,
                Notes = x.Notes,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> Create(
        SaveClientRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            ModelState.AddModelError(nameof(request.FullName), "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            ModelState.AddModelError(nameof(request.PhoneNumber), "Phone number is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var client = new Client
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new ClientResponse
        {
            Id = client.Id,
            FullName = client.FullName,
            PhoneNumber = client.PhoneNumber,
            Notes = client.Notes,
            CreatedAtUtc = client.CreatedAtUtc
        };

        return CreatedAtAction(nameof(GetById), new { id = client.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> Update(
    Guid id,
    SaveClientRequest request,
    CancellationToken cancellationToken)
    {
        var client = await _dbContext.Clients
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (client is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            ModelState.AddModelError(nameof(request.FullName), "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            ModelState.AddModelError(nameof(request.PhoneNumber), "Phone number is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        client.FullName = request.FullName.Trim();
        client.PhoneNumber = request.PhoneNumber.Trim();
        client.Notes = string.IsNullOrWhiteSpace(request.Notes)
            ? null
            : request.Notes.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new ClientResponse
        {
            Id = client.Id,
            FullName = client.FullName,
            PhoneNumber = client.PhoneNumber,
            Notes = client.Notes,
            CreatedAtUtc = client.CreatedAtUtc
        };

        return Ok(response);
    }

    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<ClientDetailsResponse>> GetDetails(
    Guid id,
    CancellationToken cancellationToken)
    {
        var client = await _dbContext.Clients
            .Include(x => x.Vehicles)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (client is null)
        {
            return NotFound();
        }

        var response = new ClientDetailsResponse
        {
            Id = client.Id,
            FullName = client.FullName,
            PhoneNumber = client.PhoneNumber,
            Notes = client.Notes,
            CreatedAtUtc = client.CreatedAtUtc,
            Vehicles = client.Vehicles
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
                .ToList()
        };

        return Ok(response);
    }
}
