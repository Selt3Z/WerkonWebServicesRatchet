using WerkonWebServicesRatchet.Contracts.System;

namespace WerkonWebServicesRatchet.Infrastructure.Backups;

public sealed class BackupCatalogService
{
    private readonly string _backupsRoot;

    public BackupCatalogService(IConfiguration configuration)
    {
        _backupsRoot = configuration["Backup:BackupsDirectory"]
            ?? Path.GetDirectoryName(configuration["Backup:StatusFilePath"] ?? string.Empty)
            ?? "backups";
    }

    public string BackupsRoot => Path.GetFullPath(_backupsRoot);

    public IReadOnlyList<BackupFileInfoResponse> ListAvailable()
    {
        if (!Directory.Exists(BackupsRoot))
        {
            return [];
        }

        var files = new List<BackupFileInfoResponse>();
        Collect(files, Path.Combine(BackupsRoot, "daily"), "daily");
        Collect(files, Path.Combine(BackupsRoot, "weekly"), "weekly");

        return files
            .OrderByDescending(x => x.CreatedUtc)
            .ToList();
    }

    public bool TryResolveSafePath(string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(BackupsRoot, normalized));
        if (!candidate.StartsWith(BackupsRoot, StringComparison.OrdinalIgnoreCase)
            || !File.Exists(candidate)
            || !candidate.EndsWith(".sql.gz", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private static void Collect(List<BackupFileInfoResponse> files, string directory, string folder)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "ratchet*.sql.gz"))
        {
            var info = new FileInfo(path);
            files.Add(new BackupFileInfoResponse
            {
                RelativePath = $"{folder}/{info.Name}",
                FileName = info.Name,
                Folder = folder,
                SizeBytes = info.Length,
                CreatedUtc = info.LastWriteTimeUtc.ToString("O")
            });
        }
    }
}
