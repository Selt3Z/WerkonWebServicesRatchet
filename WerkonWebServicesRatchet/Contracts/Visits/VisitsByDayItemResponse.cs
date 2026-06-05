namespace WerkonWebServicesRatchet.Contracts.Visits;

public sealed class VisitsByDayItemResponse
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid ClientId { get; set; }

    public DateTime VisitedAtUtc { get; set; }
    public int? MileageAtVisit { get; set; }
    public string CustomerComplaint { get; set; } = string.Empty;
    public int Status { get; set; }

    public string ClientFullName { get; set; } = string.Empty;
    public string ClientPhoneNumber { get; set; } = string.Empty;

    public string VehicleBrand { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
}