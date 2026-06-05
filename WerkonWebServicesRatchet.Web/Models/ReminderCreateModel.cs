using WerkonWebServicesRatchet.Web.Services;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class ReminderCreateModel
{
    public Guid VehicleId { get; set; }
    public Guid? VisitId { get; set; }

    [LocalizedRequired(MessageKey = "Validation_ReminderDateRequired")]
    public DateTime? ReminderDateLocal { get; set; }

    public string Note { get; set; } = string.Empty;
}
