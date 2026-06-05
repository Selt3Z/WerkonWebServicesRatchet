using WerkonWebServicesRatchet.Web.Services;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class VehicleSaveModel
{
    [LocalizedRequired(MessageKey = "Validation_BrandRequired")]
    public string Brand { get; set; } = string.Empty;

    [LocalizedRequired(MessageKey = "Validation_ModelRequired")]
    public string Model { get; set; } = string.Empty;

    public int? Year { get; set; }

    [LocalizedRequired(MessageKey = "Validation_LicensePlateRequired")]
    public string LicensePlate { get; set; } = string.Empty;

    public string? Vin { get; set; }
}
