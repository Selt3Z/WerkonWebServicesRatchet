namespace WerkonWebServicesRatchet.Contracts.Reminders;

public sealed class ReminderByDayItemResponse
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid ClientId { get; set; }

    public DateTime ReminderAtUtc { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool IsClosed { get; set; }

    public string ClientFullName { get; set; } = string.Empty;
    public string ClientPhoneNumber { get; set; } = string.Empty;

    public string VehicleBrand { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
}
