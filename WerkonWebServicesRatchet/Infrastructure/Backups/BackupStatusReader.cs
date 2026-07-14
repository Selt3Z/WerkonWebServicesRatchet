using System.Text.Json;
using WerkonWebServicesRatchet.Contracts.Settings;

namespace WerkonWebServicesRatchet.Infrastructure.Backups;

public sealed class BackupStatusReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string? _statusFilePath;

    public BackupStatusReader(IConfiguration configuration)
    {
        _statusFilePath = configuration["Backup:StatusFilePath"];
    }

    public async Task<BackupStatusResponse> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_statusFilePath) || !File.Exists(_statusFilePath))
        {
            return new BackupStatusResponse
            {
                IsConfigured = false,
                LastMessage = "Backup status file is not available."
            };
        }

        try
        {
            await using var stream = File.OpenRead(_statusFilePath);
            var payload = await JsonSerializer.DeserializeAsync<BackupStatusFile>(stream, JsonOptions, cancellationToken);

            if (payload is null)
            {
                return new BackupStatusResponse
                {
                    IsConfigured = true,
                    LastStatus = "unknown",
                    LastMessage = "Backup status file is empty."
                };
            }

            return new BackupStatusResponse
            {
                IsConfigured = true,
                LastRunUtc = payload.LastRunUtc,
                LastStatus = payload.LastStatus,
                LastMessage = payload.LastMessage,
                LastBackupSizeBytes = payload.LastBackupSizeBytes,
                ResticEnabled = payload.ResticEnabled
            };
        }
        catch (Exception ex)
        {
            return new BackupStatusResponse
            {
                IsConfigured = true,
                LastStatus = "error",
                LastMessage = ex.Message
            };
        }
    }

    private sealed class BackupStatusFile
    {
        public string? LastRunUtc { get; set; }

        public string? LastStatus { get; set; }

        public string? LastMessage { get; set; }

        public long? LastBackupSizeBytes { get; set; }

        public bool ResticEnabled { get; set; }
    }
}
