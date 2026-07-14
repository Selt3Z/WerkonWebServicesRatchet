using WerkonWebServicesRatchet.Domain.Entities;

namespace WerkonWebServicesRatchet.Contracts.Visits;

public sealed class VisitResponse
{
    public Guid Id { get; set; }
    public long Number { get; set; }
    public Guid VehicleId { get; set; }
    public DateTime VisitedAtUtc { get; set; }
    public int? MileageAtVisit { get; set; }
    public string CustomerComplaint { get; set; } = string.Empty;
    public string? MechanicComment { get; set; }
    public Guid? AssignedMechanicUserId { get; set; }
    public string? AssignedMechanicDisplayName { get; set; }
    public VisitStatus Status { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}