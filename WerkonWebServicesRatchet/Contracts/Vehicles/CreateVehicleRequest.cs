namespace WerkonWebServicesRatchet.Contracts.Vehicles;

public sealed class CreateVehicleRequest
{
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Vin { get; set; }
}