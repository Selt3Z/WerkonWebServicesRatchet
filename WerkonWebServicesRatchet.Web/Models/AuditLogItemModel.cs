namespace WerkonWebServicesRatchet.Web.Models;

public sealed class AuditLogItemModel
{
    public Guid Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string UserDisplayName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string? EntityUrl { get; set; }
}
