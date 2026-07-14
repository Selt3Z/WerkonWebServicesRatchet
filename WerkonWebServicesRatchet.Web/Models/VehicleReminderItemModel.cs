namespace WerkonWebServicesRatchet.Web.Models;

public sealed class VehicleReminderItemModel
{
    public Guid Id { get; set; }

    public DateTime ReminderAtUtc { get; set; }

    public string Note { get; set; } = string.Empty;

    public bool IsClosed { get; set; }
}
