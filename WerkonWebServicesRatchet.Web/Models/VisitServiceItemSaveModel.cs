using WerkonWebServicesRatchet.Web.Services;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class VisitServiceItemSaveModel
{
    [LocalizedRequired(MessageKey = "Validation_NameRequired")]
    public string Name { get; set; } = string.Empty;

    [LocalizedRange(0.01, double.MaxValue, MessageKey = "Validation_QuantityPositive")]
    public decimal Quantity { get; set; } = 1;

    [LocalizedRange(0, double.MaxValue, MessageKey = "Validation_UnitPriceNegative")]
    public decimal UnitPrice { get; set; }

    public string? Comment { get; set; }
}
