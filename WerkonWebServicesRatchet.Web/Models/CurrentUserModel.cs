namespace WerkonWebServicesRatchet.Web.Models;

public sealed class CurrentUserModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}
