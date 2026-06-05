namespace WerkonWebServicesRatchet.Web.Models;

public sealed class UserListItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
