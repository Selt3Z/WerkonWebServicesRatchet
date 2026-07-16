using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.System;
using WerkonWebServicesRatchet.Infrastructure.Backups;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Persistence;
using WerkonWebServicesRatchet.Infrastructure.Settings;
using WerkonWebServicesRatchet.Infrastructure.Time;

namespace WerkonWebServicesRatchet.Features.System;

[ApiController]
[Route("api/system")]
[Authorize(Policy = AuthorizationPolicies.ManageUsers)]
public sealed class SystemStatusController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly AppSettingsService _appSettingsService;
    private readonly AppTimeZone _appTimeZone;
    private readonly BackupStatusReader _backupStatusReader;
    private readonly BackupCatalogService _backupCatalog;
    private readonly DatabaseRestoreService _databaseRestore;
    private readonly NetworkInfoReader _networkInfoReader;

    public SystemStatusController(
        AppDbContext dbContext,
        AppSettingsService appSettingsService,
        AppTimeZone appTimeZone,
        BackupStatusReader backupStatusReader,
        BackupCatalogService backupCatalog,
        DatabaseRestoreService databaseRestore,
        NetworkInfoReader networkInfoReader)
    {
        _dbContext = dbContext;
        _appSettingsService = appSettingsService;
        _appTimeZone = appTimeZone;
        _backupStatusReader = backupStatusReader;
        _backupCatalog = backupCatalog;
        _databaseRestore = databaseRestore;
        _networkInfoReader = networkInfoReader;
    }

    [HttpGet("status")]
    public async Task<ActionResult<SystemStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var databaseHealthy = await _dbContext.Database.CanConnectAsync(cancellationToken);
        var auditCount = databaseHealthy
            ? await _dbContext.AuditLogEntries.CountAsync(cancellationToken)
            : 0;
        var timeZoneId = await _appSettingsService.GetTimeZoneIdAsync(cancellationToken);
        var serverUtc = DateTime.UtcNow;
        var backup = await _backupStatusReader.ReadAsync(cancellationToken);
        var network = _networkInfoReader.Read();

        return new SystemStatusResponse
        {
            ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            DatabaseHealthy = databaseHealthy,
            ServerTimeUtc = serverUtc.ToString("O"),
            AppTimeZoneId = timeZoneId,
            ServerLocalTime = _appTimeZone.FromUtc(serverUtc).ToString("yyyy-MM-dd HH:mm:ss"),
            AuditLogEntryCount = auditCount,
            MachineName = network.MachineName,
            PublicHostname = network.PublicHostname,
            AdvertiseAddresses = network.AdvertiseAddresses,
            InterfaceAddresses = network.InterfaceAddresses,
            Backup = backup,
            AvailableBackups = _backupCatalog.ListAvailable().ToList()
        };
    }

    [HttpPost("restore-backup")]
    public async Task<IActionResult> RestoreBackup(
        [FromBody] RestoreBackupRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Confirmation?.Trim(), "RESTORE", StringComparison.Ordinal))
        {
            return BadRequest("Type RESTORE to confirm.");
        }

        if (!_backupCatalog.TryResolveSafePath(request.RelativePath, out var fullPath))
        {
            return BadRequest("Backup file was not found.");
        }

        await _databaseRestore.RestoreAsync(fullPath, cancellationToken);
        return Ok(new { message = "Database restored. Sign in again if the session was dropped." });
    }
}
