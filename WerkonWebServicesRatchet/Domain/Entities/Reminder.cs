namespace WerkonWebServicesRatchet.Domain.Entities;

public sealed class Reminder
{
    public Guid Id { get; set; }

    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public Guid? VisitId { get; set; }
    public Visit? Visit { get; set; }

    public DateTime ReminderAtUtc { get; set; }
    public string Note { get; set; } = string.Empty;

    public bool IsClosed { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
