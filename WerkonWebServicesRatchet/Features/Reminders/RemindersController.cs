using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                VisitId = x.VisitId,
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

        if (request.VisitId.HasValue)
        {
            var visitValid = await _dbContext.Visits
                .AnyAsync(x => x.Id == request.VisitId.Value && x.VehicleId == request.VehicleId, cancellationToken);

            if (!visitValid)
            {
                return BadRequest("Visit does not belong to the specified vehicle.");
            }
        }

        if (request.ReminderDate == default)
        {
            return BadRequest("Reminder date is required.");
        }

        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            VehicleId = request.VehicleId,
            VisitId = request.VisitId,
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

    private IQueryable<ReminderDetailsResponse> MapDetailsQuery() =>
        _dbContext.Reminders
            .Select(x => new ReminderDetailsResponse
            {
                Id = x.Id,
                VehicleId = x.VehicleId,
                ClientId = x.Vehicle.ClientId,
                VisitId = x.VisitId,
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
