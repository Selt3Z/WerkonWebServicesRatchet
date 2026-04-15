namespace WerkonWebServicesRatchet.Web.Models;

public sealed class VisitDetailsModel
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public DateTime VisitedAtUtc { get; set; }
    public int? MileageAtVisit { get; set; }
    public string CustomerComplaint { get; set; } = string.Empty;
    public string? MechanicComment { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<VisitServiceItemModel> ServiceItems { get; set; } = [];
    public decimal TotalAmount { get; set; }
}