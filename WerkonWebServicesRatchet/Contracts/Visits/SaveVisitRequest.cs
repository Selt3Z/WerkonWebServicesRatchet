namespace WerkonWebServicesRatchet.Contracts.Visits;

public sealed class SaveVisitRequest
{
    public DateTime VisitedAtUtc { get; set; }
    public int? MileageAtVisit { get; set; }
    public string CustomerComplaint { get; set; } = string.Empty;
    public string? MechanicComment { get; set; }
}