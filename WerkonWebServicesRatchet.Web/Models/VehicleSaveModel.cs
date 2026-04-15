using System.ComponentModel.DataAnnotations;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class VehicleSaveModel
{
    [Required(ErrorMessage = "Brand is required.")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    public string Model { get; set; } = string.Empty;

    public int? Year { get; set; }

    [Required(ErrorMessage = "License plate is required.")]
    public string LicensePlate { get; set; } = string.Empty;

    public string? Vin { get; set; }
}