using WerkonWebServicesRatchet.Contracts.Vehicles;

namespace WerkonWebServicesRatchet.Contracts.Clients;

public sealed class ClientDetailsResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<VehicleResponse> Vehicles { get; set; } = [];
}