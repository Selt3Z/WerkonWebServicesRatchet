using WerkonWebServicesRatchet.Domain.Entities;

namespace WerkonWebServicesRatchet.Contracts.Visits;

public sealed class VisitDetailsResponse
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public DateTime VisitedAtUtc { get; set; }
    public int? MileageAtVisit { get; set; }
    public string CustomerComplaint { get; set; } = string.Empty;
    public string? MechanicComment { get; set; }
    public VisitStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<VisitServiceItemResponse> ServiceItems { get; set; } = [];
    public decimal TotalAmount { get; set; }
}