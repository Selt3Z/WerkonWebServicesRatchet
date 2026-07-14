namespace WerkonWebServicesRatchet.Infrastructure.Identity;

public static class AuthorizationPolicies
{
    public const string BusinessData = "BusinessData";
    public const string DeleteServiceItems = "DeleteServiceItems";
    public const string ManageUsers = "ManageUsers";
    public const string ManageServiceCatalog = "ManageServiceCatalog";
    public const string CreateCatalogService = "CreateCatalogService";
    public const string AssignVisitMechanic = "AssignVisitMechanic";
    public const string ViewAuditLog = "ViewAuditLog";

    public const string HardDeleteRecords = "HardDeleteRecords";
}
