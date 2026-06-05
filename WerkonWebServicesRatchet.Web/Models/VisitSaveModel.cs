using WerkonWebServicesRatchet.Web.Services;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class VisitSaveModel
{
    [LocalizedRequired(MessageKey = "Validation_VisitDateRequired")]
    public DateTime? VisitedAtLocal { get; set; } = DateTime.Now;

    [LocalizedRange(0, double.MaxValue, MessageKey = "Validation_MileageNegative")]
    public int? MileageAtVisit { get; set; }

    [LocalizedRequired(MessageKey = "Validation_ComplaintRequired")]
    public string CustomerComplaint { get; set; } = string.Empty;

    public string? MechanicComment { get; set; }
}
