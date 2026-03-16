namespace WerkonWebServicesRatchet.Contracts.Vehicles;

public sealed class VehicleResponse
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Vin { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}