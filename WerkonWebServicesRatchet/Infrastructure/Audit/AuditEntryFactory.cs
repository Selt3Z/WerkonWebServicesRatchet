using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Persistence;
using WerkonWebServicesRatchet.Infrastructure.Time;

namespace WerkonWebServicesRatchet.Infrastructure.Audit;

public sealed class AuditEntryFactory
{
    private readonly AppTimeZone _appTimeZone;

    public AuditEntryFactory(AppTimeZone appTimeZone)
    {
        _appTimeZone = appTimeZone;
    }

    public async Task<List<AuditLogEntry>> CreateEntriesAsync(
        AppDbContext dbContext,
        IEnumerable<EntityEntry> entries,
        Guid? userId,
        string userDisplayName,
        CancellationToken cancellationToken)
    {
        var auditEntries = new List<AuditLogEntry>();
        var occurredAtUtc = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLogEntry)
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => AuditActions.Created,
                EntityState.Modified => GetModifiedAction(entry),
                EntityState.Deleted => AuditActions.Deleted,
                _ => null
            };

            if (action is null)
            {
                continue;
            }

            if (!TryMapEntry(entry, action, out var entityType, out var entityId, out var summary, out var entityUrl))
            {
                continue;
            }

            if (entityType == AuditEntityTypes.Reminder)
            {
                summary = await EnrichReminderSummaryAsync(dbContext, entry, summary, cancellationToken);
            }

            auditEntries.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = occurredAtUtc,
                UserId = userId,
                UserDisplayName = userDisplayName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Summary = summary,
                EntityUrl = entityUrl
            });
        }

        return auditEntries;
    }

    private static string GetModifiedAction(EntityEntry entry)
    {
        var isArchivedProperty = entry.Metadata.FindProperty("IsArchived") is null
            ? null
            : entry.Property("IsArchived");

        if (isArchivedProperty is { IsModified: true } && isArchivedProperty.CurrentValue is bool isArchived)
        {
            return isArchived ? AuditActions.Archived : AuditActions.Restored;
        }

        return AuditActions.Updated;
    }

    private bool TryMapEntry(
        EntityEntry entry,
        string action,
        out string entityType,
        out Guid entityId,
        out string summary,
        out string? entityUrl)
    {
        entityType = string.Empty;
        entityId = Guid.Empty;
        summary = string.Empty;
        entityUrl = null;

        switch (entry.Entity)
        {
            case Client client:
                entityType = AuditEntityTypes.Client;
                entityId = GetEntityId(entry, client.Id);
                summary = GetStringValue(entry, nameof(Client.FullName), client.FullName);
                entityUrl = action == AuditActions.Deleted ? null : $"/clients/{entityId}";
                return true;

            case Vehicle vehicle:
                entityType = AuditEntityTypes.Vehicle;
                entityId = GetEntityId(entry, vehicle.Id);
                summary = GetStringValue(entry, nameof(Vehicle.LicensePlate), vehicle.LicensePlate);
                entityUrl = action == AuditActions.Deleted ? null : $"/vehicles/{entityId}";
                return true;

            case Visit visit:
                entityType = AuditEntityTypes.Visit;
                entityId = GetEntityId(entry, visit.Id);
                summary = FormatVisitSummary(GetDateTimeValue(entry, nameof(Visit.VisitedAtUtc), visit.VisitedAtUtc));
                entityUrl = action == AuditActions.Deleted ? null : $"/visits/{entityId}";
                return true;

            case VisitServiceItem serviceItem:
                entityType = AuditEntityTypes.ServiceItem;
                entityId = GetEntityId(entry, serviceItem.Id);
                summary = GetStringValue(entry, nameof(VisitServiceItem.Name), serviceItem.Name);
                var visitId = GetGuidValue(entry, nameof(VisitServiceItem.VisitId), serviceItem.VisitId);
                entityUrl = action == AuditActions.Deleted || visitId == Guid.Empty
                    ? null
                    : $"/visits/{visitId}";
                return true;

            case Reminder reminder:
                entityType = AuditEntityTypes.Reminder;
                entityId = GetEntityId(entry, reminder.Id);
                summary = FormatReminderSummary(
                    GetDateTimeValue(entry, nameof(Reminder.ReminderAtUtc), reminder.ReminderAtUtc),
                    phoneNumber: null);
                entityUrl = action == AuditActions.Deleted ? null : $"/reminders/{entityId}";
                return true;

            case CatalogService catalogService:
                entityType = AuditEntityTypes.CatalogService;
                entityId = GetEntityId(entry, catalogService.Id);
                summary = GetStringValue(entry, nameof(CatalogService.Name), catalogService.Name);
                entityUrl = action == AuditActions.Deleted ? null : $"/catalog-services/{entityId}/edit";
                return true;

            default:
                return false;
        }
    }

    private async Task<string> EnrichReminderSummaryAsync(
        AppDbContext dbContext,
        EntityEntry entry,
        string summary,
        CancellationToken cancellationToken)
    {
        string? phoneNumber = null;

        if (entry.Entity is Reminder reminder
            && entry.Reference(nameof(Reminder.Vehicle)).IsLoaded
            && reminder.Vehicle?.Client is not null)
        {
            phoneNumber = reminder.Vehicle.Client.PhoneNumber;
        }
        else
        {
            var vehicleId = GetGuidValue(entry, nameof(Reminder.VehicleId), Guid.Empty);

            phoneNumber = await dbContext.Vehicles
                .Where(x => x.Id == vehicleId)
                .Select(x => x.Client.PhoneNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var reminderAtUtc = GetDateTimeValue(
            entry,
            nameof(Reminder.ReminderAtUtc),
            entry.Entity is Reminder currentReminder ? currentReminder.ReminderAtUtc : default);

        return FormatReminderSummary(reminderAtUtc, phoneNumber);
    }

    private string FormatVisitSummary(DateTime visitedAtUtc) =>
        _appTimeZone.FromUtc(visitedAtUtc).ToString("yyyy-MM-dd HH:mm");

    private string FormatReminderSummary(DateTime reminderAtUtc, string? phoneNumber)
    {
        var datePart = _appTimeZone.FromUtc(reminderAtUtc).ToString("yyyy-MM-dd");

        return string.IsNullOrWhiteSpace(phoneNumber)
            ? datePart
            : $"{datePart} · {phoneNumber}";
    }

    private static Guid GetEntityId(EntityEntry entry, Guid currentId) =>
        entry.State == EntityState.Deleted
            ? entry.OriginalValues.GetValue<Guid>("Id")
            : currentId;

    private static string GetStringValue(EntityEntry entry, string propertyName, string currentValue) =>
        entry.State == EntityState.Deleted
            ? entry.OriginalValues.GetValue<string>(propertyName) ?? string.Empty
            : currentValue;

    private static DateTime GetDateTimeValue(EntityEntry entry, string propertyName, DateTime currentValue) =>
        entry.State == EntityState.Deleted
            ? entry.OriginalValues.GetValue<DateTime>(propertyName)
            : currentValue;

    private static Guid GetGuidValue(EntityEntry entry, string propertyName, Guid currentValue)
    {
        if (entry.State == EntityState.Deleted)
        {
            return entry.OriginalValues.GetValue<Guid>(propertyName);
        }

        return currentValue == Guid.Empty
            ? entry.CurrentValues.GetValue<Guid>(propertyName)
            : currentValue;
    }
}
