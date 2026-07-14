using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WerkonWebServicesRatchet.Contracts.Settings;
using WerkonWebServicesRatchet.Infrastructure.Backups;
using WerkonWebServicesRatchet.Infrastructure.Identity;
using WerkonWebServicesRatchet.Infrastructure.Settings;

namespace WerkonWebServicesRatchet.Features.Settings;

[ApiController]
[Route("api/settings")]
[Authorize(Policy = AuthorizationPolicies.BusinessData)]
public sealed class SettingsController : ControllerBase
{
    private readonly AppSettingsService _appSettingsService;
    private readonly BackupStatusReader _backupStatusReader;

    public SettingsController(AppSettingsService appSettingsService, BackupStatusReader backupStatusReader)
    {
        _appSettingsService = appSettingsService;
        _backupStatusReader = backupStatusReader;
    }

    [HttpGet]
    public async Task<ActionResult<SettingsResponse>> Get(CancellationToken cancellationToken)
    {
        return await _appSettingsService.GetSettingsAsync(cancellationToken);
    }

    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
    public async Task<ActionResult<SettingsResponse>> Update(
        [FromBody] UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _appSettingsService.UpdateSettingsAsync(request, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("timezone")]
    [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
    public async Task<ActionResult<SettingsResponse>> UpdateTimeZone(
        [FromBody] UpdateTimeZoneRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _appSettingsService.UpdateSettingsAsync(
                new UpdateSettingsRequest { TimeZoneId = request.TimeZoneId },
                cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("organization")]
    [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
    public async Task<ActionResult<OrganizationProfileResponse>> GetOrganization(
        CancellationToken cancellationToken)
    {
        return await _appSettingsService.GetOrganizationProfileAsync(cancellationToken);
    }

    [HttpPut("organization")]
    [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
    public async Task<ActionResult<OrganizationProfileResponse>> UpdateOrganization(
        [FromBody] UpdateOrganizationProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _appSettingsService.UpdateOrganizationProfileAsync(request, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("organization/logo")]
    [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
    public async Task<ActionResult<OrganizationProfileResponse>> UploadOrganizationLogo(
        [FromBody] UploadOrganizationLogoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _appSettingsService.UploadOrganizationLogoAsync(request, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("backup-status")]
    [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
    public async Task<ActionResult<BackupStatusResponse>> GetBackupStatus(CancellationToken cancellationToken)
    {
        return await _backupStatusReader.ReadAsync(cancellationToken);
    }

    [HttpDelete("organization/logo")]
    [Authorize(Policy = AuthorizationPolicies.ManageUsers)]
    public async Task<ActionResult<OrganizationProfileResponse>> DeleteOrganizationLogo(
        CancellationToken cancellationToken)
    {
        return await _appSettingsService.DeleteOrganizationLogoAsync(cancellationToken);
    }
}
