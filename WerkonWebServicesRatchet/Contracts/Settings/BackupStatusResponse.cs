namespace WerkonWebServicesRatchet.Contracts.Settings;

public sealed class BackupStatusResponse
{
    public bool IsConfigured { get; set; }

    public string? LastRunUtc { get; set; }

    public string? LastStatus { get; set; }

    public string? LastMessage { get; set; }

    public long? LastBackupSizeBytes { get; set; }

    public bool ResticEnabled { get; set; }

    public bool BackupOkToday { get; set; }
}
