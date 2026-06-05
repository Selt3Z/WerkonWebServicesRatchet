using WerkonWebServicesRatchet.Web.Services;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class CatalogServiceSaveModel
{
    [LocalizedRequired(MessageKey = "Validation_ServiceNameRequired")]
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Category { get; set; }

    [LocalizedRange(0, double.MaxValue, MessageKey = "Validation_UnitPriceNegative")]
    public decimal DefaultUnitPrice { get; set; }

    [LocalizedRequired(MessageKey = "Validation_ServiceUnitRequired")]
    public string DefaultUnit { get; set; } = "шт";

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
