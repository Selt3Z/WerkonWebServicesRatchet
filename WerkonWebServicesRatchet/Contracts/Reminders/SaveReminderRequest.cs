namespace WerkonWebServicesRatchet.Contracts.Reminders;

public sealed class SaveReminderRequest
{
    public Guid VehicleId { get; set; }

    public DateOnly ReminderDate { get; set; }

    public string Note { get; set; } = string.Empty;
}
