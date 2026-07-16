namespace WerkonWebServicesRatchet.Contracts.Search;

public sealed class GlobalSearchResponse
{
    public List<GlobalSearchHit> Clients { get; set; } = [];

    public List<GlobalSearchHit> Vehicles { get; set; } = [];
}

public sealed class GlobalSearchHit
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;
}
