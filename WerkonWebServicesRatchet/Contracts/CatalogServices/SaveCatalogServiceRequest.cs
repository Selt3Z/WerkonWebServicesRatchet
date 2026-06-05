namespace WerkonWebServicesRatchet.Contracts.CatalogServices;

public sealed class SaveCatalogServiceRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Category { get; set; }

    public decimal DefaultUnitPrice { get; set; }

    public string DefaultUnit { get; set; } = "шт";

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
