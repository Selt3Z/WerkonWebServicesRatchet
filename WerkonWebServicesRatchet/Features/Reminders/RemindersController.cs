using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Common;
using WerkonWebServicesRatchet.Contracts.Reminders;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Persistence;
using WerkonWebServicesRatchet.Infrastructure.Time;

namespace WerkonWebServicesRatchet.Features.Reminders;

[ApiController]
[Route("api/reminders")]
[Authorize(Policy = AuthorizationPolicies.BusinessData)]
public sealed class RemindersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly AppTimeZone _appTimeZone;

    public RemindersController(AppDbContext dbContext, AppTimeZone appTimeZone)
    {
        _dbContext = dbContext;
        _appTimeZone = appTimeZone;
    }

    /// <summary>
    /// status: open | closed | all
    /// scope: overdue | today | upcoming | all
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ReminderByDayItemResponse>>> GetAll(
        [FromQuery] string? status = "open",
        [FromQuery] string? scope = "all",
        [FromQuery] string? q = null,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null,
        CancellationToken cancellationToken = default)
    {
        var today = _appTimeZone.GetToday();
        var (todayStartUtc, todayEndUtc) = _appTimeZone.GetDayRangeUtc(today);

        var query = _dbContext.Reminders.AsQueryable();

        var normalizedStatus = (status ?? "open").Trim().ToLowerInvariant();
        query = normalizedStatus switch
        {
            "closed" => query.Where(x => x.IsClosed),
            "all" => query,
            _ => query.Where(x => !x.IsClosed)
        };

        var normalizedScope = (scope ?? "all").Trim().ToLowerInvariant();
        query = normalizedScope switch
        {
            "overdue" => query.Where(x => !x.IsClosed && x.ReminderAtUtc < todayStartUtc),
            "today" => query.Where(x => x.ReminderAtUtc >= todayStartUtc && x.ReminderAtUtc < todayEndUtc),
            "upcoming" => query.Where(x => x.ReminderAtUtc >= todayEndUtc),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x =>
                x.Note.ToLower().Contains(term)
                || x.Vehicle.LicensePlate.ToLower().Contains(term)
                || x.Vehicle.Client.FullName.ToLower().Contains(term)
                || x.Vehicle.Client.PhoneNumber.ToLower().Contains(term)
                || (x.Vehicle.Vin != null && x.Vehicle.Vin.ToLower().Contains(term)));
        }

        var (normalizedSkip, normalizedTake) = QueryPagingExtensions.NormalizePaging(skip, take);

        var projected = query.Select(x => new ReminderByDayItemResponse
        {
            Id = x.Id,
            VehicleId = x.VehicleId,
            ClientId = x.Vehicle.ClientId,
            ReminderAtUtc = x.ReminderAtUtc,
            Note = x.Note,
            IsClosed = x.IsClosed,
            ClientFullName = x.Vehicle.Client.FullName,
            ClientPhoneNumber = x.Vehicle.Client.PhoneNumber,
            VehicleBrand = x.Vehicle.Brand,
            VehicleModel = x.Vehicle.Model,
            LicensePlate = x.Vehicle.LicensePlate
        });

        var ordered = normalizedScope == "overdue"
            ? projected.OrderBy(x => x.ReminderAtUtc).ThenBy(x => x.Id)
            : projected.OrderByDescending(x => x.ReminderAtUtc).ThenBy(x => x.Id);

        var response = await ordered.ToPagedResponseAsync(normalizedSkip, normalizedTake, cancellationToken);
        return Ok(response);
    }

    [HttpGet("by-day")]
    public async Task<ActionResult<List<ReminderByDayItemResponse>>> GetByDay(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var selectedDate = date ?? _appTimeZone.GetToday();
        var (startUtc, endUtc) = _appTimeZone.GetDayRangeUtc(selectedDate);

        var response = await _dbContext.Reminders
            .Where(x => x.ReminderAtUtc >= startUtc && x.ReminderAtUtc < endUtc)
            .OrderBy(x => x.ReminderAtUtc)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(x => new ReminderByDayItemResponse
            {
                Id = x.Id,
                VehicleId = x.VehicleId,
                ClientId = x.Vehicle.ClientId,
                ReminderAtUtc = x.ReminderAtUtc,
                Note = x.Note,
                IsClosed = x.IsClosed,
                ClientFullName = x.Vehicle.Client.FullName,
                ClientPhoneNumber = x.Vehicle.Client.PhoneNumber,
                VehicleBrand = x.Vehicle.Brand,
                VehicleModel = x.Vehicle.Model,
                LicensePlate = x.Vehicle.LicensePlate
            })
            .ToListAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("by-range")]
    public async Task<ActionResult<List<ReminderByDayItemResponse>>> GetByRange(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var endDate = to ?? _appTimeZone.GetToday();
        var startDate = from ?? endDate;

        if (startDate > endDate)
        {
            return BadRequest("'from' must be on or before 'to'.");
        }

        var (startUtc, _) = _appTimeZone.GetDayRangeUtc(startDate);
        var (_, endUtc) = _appTimeZone.GetDayRangeUtc(endDate);

        var response = await _dbContext.Reminders
            .Where(x => x.ReminderAtUtc >= startUtc && x.ReminderAtUtc < endUtc)
            .OrderBy(x => x.ReminderAtUtc)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(x => new ReminderByDayItemResponse
            {
                Id = x.Id,
                VehicleId = x.VehicleId,
                ClientId = x.Vehicle.ClientId,
                ReminderAtUtc = x.ReminderAtUtc,
                Note = x.Note,
                IsClosed = x.IsClosed,
                ClientFullName = x.Vehicle.Client.FullName,
                ClientPhoneNumber = x.Vehicle.Client.PhoneNumber,
                VehicleBrand = x.Vehicle.Brand,
                VehicleModel = x.Vehicle.Model,
                LicensePlate = x.Vehicle.LicensePlate
            })
            .ToListAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReminderDetailsResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await MapDetailsQuery()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ReminderDetailsResponse>> Create(
        SaveReminderRequest request,
        CancellationToken cancellationToken)
    {
        var vehicleExists = await _dbContext.Vehicles
            .AnyAsync(x => x.Id == request.VehicleId, cancellationToken);

        if (!vehicleExists)
        {
            return NotFound("Vehicle not found.");
        }

        if (request.ReminderDate == default)
        {
            return BadRequest("Reminder date is required.");
        }

        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            VehicleId = request.VehicleId,
            ReminderAtUtc = _appTimeZone.ToUtcStartOfDay(request.ReminderDate),
            Note = request.Note.Trim(),
            IsClosed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Reminders.Add(reminder);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await MapDetailsQuery()
            .SingleAsync(x => x.Id == reminder.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = reminder.Id }, response);
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Manager},{AppRoles.Mechanic}")]
    [HttpPatch("{id:guid}/close")]
    public async Task<ActionResult<ReminderDetailsResponse>> Close(
        Guid id,
        CancellationToken cancellationToken)
    {
        var reminder = await _dbContext.Reminders
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (reminder is null)
        {
            return NotFound();
        }

        if (!reminder.IsClosed)
        {
            reminder.IsClosed = true;
            reminder.ClosedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var response = await MapDetailsQuery()
            .SingleAsync(x => x.Id == id, cancellationToken);

        return Ok(response);
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Manager},{AppRoles.Mechanic}")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var reminder = await _dbContext.Reminders
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (reminder is null)
        {
            return NotFound();
        }

        _dbContext.Reminders.Remove(reminder);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private IQueryable<ReminderDetailsResponse> MapDetailsQuery() =>
        _dbContext.Reminders
            .Select(x => new ReminderDetailsResponse
            {
                Id = x.Id,
                VehicleId = x.VehicleId,
                ClientId = x.Vehicle.ClientId,
                ReminderAtUtc = x.ReminderAtUtc,
                Note = x.Note,
                IsClosed = x.IsClosed,
                ClosedAtUtc = x.ClosedAtUtc,
                CreatedAtUtc = x.CreatedAtUtc,
                ClientFullName = x.Vehicle.Client.FullName,
                ClientPhoneNumber = x.Vehicle.Client.PhoneNumber,
                VehicleBrand = x.Vehicle.Brand,
                VehicleModel = x.Vehicle.Model,
                LicensePlate = x.Vehicle.LicensePlate
            });
}
