using System.ComponentModel.DataAnnotations;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class VisitSaveModel
{
    [Required(ErrorMessage = "Visit date and time is required.")]
    public DateTime? VisitedAtLocal { get; set; } = DateTime.Now;

    [Range(0, int.MaxValue, ErrorMessage = "Mileage cannot be negative.")]
    public int? MileageAtVisit { get; set; }

    [Required(ErrorMessage = "Customer complaint is required.")]
    public string CustomerComplaint { get; set; } = string.Empty;

    public string? MechanicComment { get; set; }
}