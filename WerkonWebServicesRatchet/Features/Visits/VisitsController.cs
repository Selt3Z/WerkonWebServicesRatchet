using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Visits;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Pdf;
using WerkonWebServicesRatchet.Infrastructure.Persistence;
using WerkonWebServicesRatchet.Infrastructure.Settings;
using WerkonWebServicesRatchet.Infrastructure.Time;

namespace WerkonWebServicesRatchet.Features.Visits;

[ApiController]
[Route("api/visits")]
[Authorize(Policy = AuthorizationPolicies.BusinessData)]
public sealed class VisitsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly AppTimeZone _appTimeZone;
    private readonly AppSettingsService _appSettingsService;
    private readonly VisitWorkOrderPdfGenerator _workOrderPdfGenerator;

    public VisitsController(
        AppDbContext dbContext,
        AppTimeZone appTimeZone,
        AppSettingsService appSettingsService,
        VisitWorkOrderPdfGenerator workOrderPdfGenerator)
    {
        _dbContext = dbContext;
        _appTimeZone = appTimeZone;
        _appSettingsService = appSettingsService;
        _workOrderPdfGenerator = workOrderPdfGenerator;
    }

    [HttpGet("~/api/vehicles/{vehicleId:guid}/visits")]
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

        var response = await ProjectVisitResponses(_dbContext.Visits.Where(x => x.VehicleId == vehicleId))
            .OrderByDescending(x => x.VisitedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VisitResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await ProjectVisitResponses(_dbContext.Visits.IgnoreQueryFilters().Where(x => x.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost("~/api/vehicles/{vehicleId:guid}/visits")]
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
            Status = await _appSettingsService.GetDefaultVisitStatusAsync(cancellationToken),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Visits.Add(visit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await MapVisitResponseAsync(visit, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = visit.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VisitResponse>> Update(
    Guid id,
    SaveVisitRequest request,
    CancellationToken cancellationToken)
    {
        var visit = await _dbContext.Visits
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (visit is null)
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

        visit.VisitedAtUtc = request.VisitedAtUtc;
        visit.MileageAtVisit = request.MileageAtVisit;
        visit.CustomerComplaint = request.CustomerComplaint.Trim();
        visit.MechanicComment = string.IsNullOrWhiteSpace(request.MechanicComment)
            ? null
            : request.MechanicComment.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await MapVisitResponseAsync(visit, cancellationToken);

        return Ok(response);
    }

    [HttpGet("mechanics")]
    [Authorize(Policy = AuthorizationPolicies.AssignVisitMechanic)]
    public async Task<ActionResult<List<MechanicListItemResponse>>> GetMechanics(
        CancellationToken cancellationToken)
    {
        var response = await (
            from user in _dbContext.Users
            join userRole in _dbContext.UserRoles on user.Id equals userRole.UserId
            join role in _dbContext.Roles on userRole.RoleId equals role.Id
            where role.Name == AppRoles.Mechanic
            orderby user.DisplayName, user.UserName
            select new MechanicListItemResponse
            {
                Id = user.Id,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName! : user.DisplayName
            }).ToListAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{id:guid}/mechanic")]
    [Authorize(Policy = AuthorizationPolicies.AssignVisitMechanic)]
    public async Task<ActionResult<VisitDetailsResponse>> AssignMechanic(
        Guid id,
        AssignVisitMechanicRequest request,
        CancellationToken cancellationToken)
    {
        var visit = await _dbContext.Visits
            .Include(x => x.ServiceItems)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (visit is null)
        {
            return NotFound();
        }

        if (request.AssignedMechanicUserId.HasValue)
        {
            var isMechanic = await IsMechanicUserAsync(request.AssignedMechanicUserId.Value, cancellationToken);

            if (!isMechanic)
            {
                return BadRequest("Selected user is not a mechanic.");
            }
        }

        visit.AssignedMechanicUserId = request.AssignedMechanicUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await MapVisitDetailsResponseAsync(visit, cancellationToken));
    }

    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<VisitDetailsResponse>> GetDetails(
    Guid id,
    CancellationToken cancellationToken) //Берёт один визит, подтягивает его работы, считает итоговую сумму и отдаёт всё разом.
    {
        var visit = await _dbContext.Visits
            .IgnoreQueryFilters()
            .Include(x => x.ServiceItems)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (visit is null)
        {
            return NotFound();
        }

        return Ok(await MapVisitDetailsResponseAsync(visit, cancellationToken));
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
        var visit = await _dbContext.Visits
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (visit is null)
        {
            return NotFound();
        }

        var hasServiceItems = await _dbContext.VisitServiceItems
            .AnyAsync(x => x.VisitId == id, cancellationToken);

        if (hasServiceItems)
        {
            return Conflict(new { message = "Visit has service items and cannot be deleted. Archive it instead." });
        }

        _dbContext.Visits.Remove(visit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
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

        var response = await MapVisitResponseAsync(visit, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}/work-order")]
    public async Task<IActionResult> GetWorkOrder(Guid id, CancellationToken cancellationToken)
    {
        var visit = await _dbContext.Visits
            .IgnoreQueryFilters()
            .Include(x => x.ServiceItems)
            .Include(x => x.Vehicle)
            .ThenInclude(x => x.Client)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.Completed)
        {
            return BadRequest("Work order is available only for completed visits.");
        }

        var items = visit.ServiceItems
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new VisitWorkOrderItemData
            {
                Name = x.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                TotalPrice = x.Quantity * x.UnitPrice,
                Comment = x.Comment
            })
            .ToList();

        var assignedMechanicDisplayName = await GetUserDisplayNameAsync(
            visit.AssignedMechanicUserId,
            cancellationToken);

        var organization = await _appSettingsService.GetOrganizationDocumentInfoAsync(cancellationToken);

        var pdf = _workOrderPdfGenerator.Generate(new VisitWorkOrderData
        {
            Organization = organization,
            VisitId = visit.Id,
            VisitNumber = visit.Number,
            VisitedAtLocal = _appTimeZone.FromUtc(visit.VisitedAtUtc),
            ClientFullName = visit.Vehicle.Client.FullName,
            ClientPhoneNumber = visit.Vehicle.Client.PhoneNumber,
            VehicleBrand = visit.Vehicle.Brand,
            VehicleModel = visit.Vehicle.Model,
            LicensePlate = visit.Vehicle.LicensePlate,
            Vin = visit.Vehicle.Vin,
            MileageAtVisit = visit.MileageAtVisit,
            CustomerComplaint = visit.CustomerComplaint,
            MechanicComment = visit.MechanicComment,
            AssignedMechanicDisplayName = assignedMechanicDisplayName,
            Items = items,
            TotalAmount = items.Sum(x => x.TotalPrice)
        });

        var fileName = $"work-order-{visit.Number}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    [HttpGet("by-day")]
    public async Task<ActionResult<List<VisitsByDayItemResponse>>> GetByDay(
    [FromQuery] DateOnly? date,
    CancellationToken cancellationToken)
    {
        var selectedDate = date ?? _appTimeZone.GetToday();
        var (startUtc, endUtc) = _appTimeZone.GetDayRangeUtc(selectedDate);

        var response = await (
            from visit in _dbContext.Visits
            where visit.VisitedAtUtc >= startUtc && visit.VisitedAtUtc < endUtc
            join mechanic in _dbContext.Users on visit.AssignedMechanicUserId equals mechanic.Id into mechanics
            from mechanic in mechanics.DefaultIfEmpty()
            orderby visit.VisitedAtUtc
            select new VisitsByDayItemResponse
            {
                Id = visit.Id,
                VehicleId = visit.VehicleId,
                ClientId = visit.Vehicle.ClientId,
                VisitedAtUtc = visit.VisitedAtUtc,
                MileageAtVisit = visit.MileageAtVisit,
                CustomerComplaint = visit.CustomerComplaint,
                Status = (int)visit.Status,
                ClientFullName = visit.Vehicle.Client.FullName,
                ClientPhoneNumber = visit.Vehicle.Client.PhoneNumber,
                VehicleBrand = visit.Vehicle.Brand,
                VehicleModel = visit.Vehicle.Model,
                LicensePlate = visit.Vehicle.LicensePlate,
                AssignedMechanicDisplayName = mechanic == null
                    ? null
                    : (string.IsNullOrWhiteSpace(mechanic.DisplayName) ? mechanic.UserName : mechanic.DisplayName)
            }).ToListAsync(cancellationToken);

        return Ok(response);
    }

    private IQueryable<VisitResponse> ProjectVisitResponses(IQueryable<Visit> visits) =>
        from visit in visits
        join mechanic in _dbContext.Users on visit.AssignedMechanicUserId equals mechanic.Id into mechanics
        from mechanic in mechanics.DefaultIfEmpty()
        select new VisitResponse
        {
            Id = visit.Id,
            Number = visit.Number,
            VehicleId = visit.VehicleId,
            VisitedAtUtc = visit.VisitedAtUtc,
            MileageAtVisit = visit.MileageAtVisit,
            CustomerComplaint = visit.CustomerComplaint,
            MechanicComment = visit.MechanicComment,
            AssignedMechanicUserId = visit.AssignedMechanicUserId,
            AssignedMechanicDisplayName = mechanic == null
                ? null
                : (string.IsNullOrWhiteSpace(mechanic.DisplayName) ? mechanic.UserName : mechanic.DisplayName),
            Status = visit.Status,
            IsArchived = visit.IsArchived,
            CreatedAtUtc = visit.CreatedAtUtc
        };

    private async Task<VisitResponse> MapVisitResponseAsync(Visit visit, CancellationToken cancellationToken)
    {
        var assignedMechanicDisplayName = await GetUserDisplayNameAsync(
            visit.AssignedMechanicUserId,
            cancellationToken);

        return new VisitResponse
        {
            Id = visit.Id,
            Number = visit.Number,
            VehicleId = visit.VehicleId,
            VisitedAtUtc = visit.VisitedAtUtc,
            MileageAtVisit = visit.MileageAtVisit,
            CustomerComplaint = visit.CustomerComplaint,
            MechanicComment = visit.MechanicComment,
            AssignedMechanicUserId = visit.AssignedMechanicUserId,
            AssignedMechanicDisplayName = assignedMechanicDisplayName,
            Status = visit.Status,
            IsArchived = visit.IsArchived,
            CreatedAtUtc = visit.CreatedAtUtc
        };
    }

    private async Task<VisitDetailsResponse> MapVisitDetailsResponseAsync(
        Visit visit,
        CancellationToken cancellationToken)
    {
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

        var assignedMechanicDisplayName = await GetUserDisplayNameAsync(
            visit.AssignedMechanicUserId,
            cancellationToken);

        return new VisitDetailsResponse
        {
            Id = visit.Id,
            Number = visit.Number,
            VehicleId = visit.VehicleId,
            VisitedAtUtc = visit.VisitedAtUtc,
            MileageAtVisit = visit.MileageAtVisit,
            CustomerComplaint = visit.CustomerComplaint,
            MechanicComment = visit.MechanicComment,
            AssignedMechanicUserId = visit.AssignedMechanicUserId,
            AssignedMechanicDisplayName = assignedMechanicDisplayName,
            Status = visit.Status,
            IsArchived = visit.IsArchived,
            CreatedAtUtc = visit.CreatedAtUtc,
            HasDependentRecords = serviceItems.Count > 0,
            ServiceItems = serviceItems,
            TotalAmount = serviceItems.Sum(x => x.TotalPrice)
        };
    }

    private async Task<IActionResult> SetArchivedAsync(
        Guid id,
        bool archived,
        CancellationToken cancellationToken)
    {
        var visit = await _dbContext.Visits
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (visit is null)
        {
            return NotFound();
        }

        if (visit.IsArchived != archived)
        {
            visit.IsArchived = archived;
            visit.ArchivedAtUtc = archived ? DateTime.UtcNow : null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    private async Task<string?> GetUserDisplayNameAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await _dbContext.Users
            .Where(x => x.Id == userId.Value)
            .Select(x => new { x.DisplayName, x.UserName })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName;
    }

    private async Task<bool> IsMechanicUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await (
            from userRole in _dbContext.UserRoles
            join role in _dbContext.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId && role.Name == AppRoles.Mechanic
            select userRole.UserId).AnyAsync(cancellationToken);
}