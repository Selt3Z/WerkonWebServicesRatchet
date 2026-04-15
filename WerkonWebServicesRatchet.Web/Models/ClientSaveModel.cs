using System.ComponentModel.DataAnnotations;

namespace WerkonWebServicesRatchet.Web.Models;

public sealed class ClientSaveModel
{
    [Required(ErrorMessage = "Full name is required.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Notes { get; set; }
}