namespace WerkonWebServicesRatchet.Infrastructure.Identity;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Mechanic = "Mechanic";

    public static readonly string[] All =
    [
        Administrator,
        Manager,
        Mechanic
    ];

    public static readonly string[] BusinessUsers =
    [
        Administrator,
        Manager,
        Mechanic
    ];

    public static readonly string[] CanDeleteServiceItems =
    [
        Administrator,
        Manager,
        Mechanic
    ];

    public static readonly string[] CanManageUsers =
    [
        Administrator
    ];

    public static readonly string[] CanManageServiceCatalog =
    [
        Administrator,
        Manager
    ];

    public static readonly string[] CanViewAuditLog =
    [
        Administrator,
        Manager
    ];

    public static readonly string[] CanAssignVisitMechanic =
    [
        Administrator,
        Manager
    ];
}
