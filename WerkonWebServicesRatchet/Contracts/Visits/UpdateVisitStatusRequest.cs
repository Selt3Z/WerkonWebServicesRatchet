using WerkonWebServicesRatchet.Domain.Entities;

namespace WerkonWebServicesRatchet.Contracts.Visits;

public sealed class UpdateVisitStatusRequest
{
    public VisitStatus Status { get; set; }
}