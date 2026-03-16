namespace WerkonWebServicesRatchet.Domain.Entities;

public sealed class Vehicle
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }

    public string LicensePlate { get; set; } = string.Empty;
    public string? Vin { get; set; }
    public List<Visit> Visits { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }
}