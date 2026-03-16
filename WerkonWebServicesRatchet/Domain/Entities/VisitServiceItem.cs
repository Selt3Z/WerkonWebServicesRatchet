namespace WerkonWebServicesRatchet.Domain.Entities;

public sealed class VisitServiceItem
{
    public Guid Id { get; set; }

    public Guid VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}