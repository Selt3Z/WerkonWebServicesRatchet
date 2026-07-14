namespace WerkonWebServicesRatchet.Web.Models;

public sealed class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public bool HasMore { get; set; }
}
