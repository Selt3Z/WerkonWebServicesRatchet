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

    public SystemStatusController(
        AppDbContext dbContext,
        AppSettingsService appSettingsService,
        AppTimeZone appTimeZone,
        BackupStatusReader backupStatusReader)
    {
        _dbContext = dbContext;
        _appSettingsService = appSettingsService;
        _appTimeZone = appTimeZone;
        _backupStatusReader = backupStatusReader;
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

        return new SystemStatusResponse
        {
            ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            DatabaseHealthy = databaseHealthy,
            ServerTimeUtc = serverUtc.ToString("O"),
            AppTimeZoneId = timeZoneId,
            ServerLocalTime = _appTimeZone.FromUtc(serverUtc).ToString("yyyy-MM-dd HH:mm:ss"),
            AuditLogEntryCount = auditCount,
            Backup = backup
        };
    }
}
