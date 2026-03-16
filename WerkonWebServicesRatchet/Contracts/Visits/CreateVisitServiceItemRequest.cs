namespace WerkonWebServicesRatchet.Contracts.Visits;

public sealed class CreateVisitServiceItemRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Comment { get; set; }
}