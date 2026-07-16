namespace WerkonWebServicesRatchet.Web.Models;

public sealed class GlobalSearchResult
{
    public List<GlobalSearchHitModel> Clients { get; set; } = [];

    public List<GlobalSearchHitModel> Vehicles { get; set; } = [];
}

public sealed class GlobalSearchHitModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;
}

public sealed class BackupFileInfoModel
{
    public string RelativePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string Folder { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string CreatedUtc { get; set; } = string.Empty;
}
