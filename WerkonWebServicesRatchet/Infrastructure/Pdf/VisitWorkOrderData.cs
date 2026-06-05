namespace WerkonWebServicesRatchet.Infrastructure.Pdf;

public sealed class VisitWorkOrderData
{
    public OrganizationDocumentInfo Organization { get; init; } = new();

    public Guid VisitId { get; init; }

    public DateTime VisitedAtLocal { get; init; }

    public string ClientFullName { get; init; } = string.Empty;

    public string ClientPhoneNumber { get; init; } = string.Empty;

    public string VehicleBrand { get; init; } = string.Empty;

    public string VehicleModel { get; init; } = string.Empty;

    public string LicensePlate { get; init; } = string.Empty;

    public string? Vin { get; init; }

    public int? MileageAtVisit { get; init; }

    public string CustomerComplaint { get; init; } = string.Empty;

    public string? MechanicComment { get; init; }

    public string? AssignedMechanicDisplayName { get; init; }

    public IReadOnlyList<VisitWorkOrderItemData> Items { get; init; } = [];

    public decimal TotalAmount { get; init; }
}

public sealed class VisitWorkOrderItemData
{
    public string Name { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal TotalPrice { get; init; }

    public string? Comment { get; init; }
}
