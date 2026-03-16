namespace WerkonWebServicesRatchet.Domain.Entities;

public sealed class Visit
{
    public Guid Id { get; set; }

    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public DateTime VisitedAtUtc { get; set; }
    public int? MileageAtVisit { get; set; }

    public string CustomerComplaint { get; set; } = string.Empty;
    public string? MechanicComment { get; set; }

    public VisitStatus Status { get; set; }
    public List<VisitServiceItem> ServiceItems { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }
}