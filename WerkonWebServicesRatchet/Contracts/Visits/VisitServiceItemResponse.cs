namespace WerkonWebServicesRatchet.Contracts.Visits;

public sealed class VisitServiceItemResponse
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}