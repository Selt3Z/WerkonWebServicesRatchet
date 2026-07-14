namespace WerkonWebServicesRatchet.Web.Models;

public sealed class BackupStatusModel
{
    public bool IsConfigured { get; set; }

    public string? LastRunUtc { get; set; }

    public string? LastStatus { get; set; }

    public string? LastMessage { get; set; }

    public long? LastBackupSizeBytes { get; set; }

    public bool ResticEnabled { get; set; }
}
