namespace WerkonWebServicesRatchet.Web.Models;

public sealed class ClientDetailsModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<VehicleListItem> Vehicles { get; set; } = [];
}