namespace WerkonWebServicesRatchet.Contracts.Auth;

public sealed class SaveUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
