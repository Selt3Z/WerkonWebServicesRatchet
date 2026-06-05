using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WerkonWebServicesRatchet.Contracts.Auth;
using WerkonWebServicesRatchet.Infrastructure.Identity;

namespace WerkonWebServicesRatchet.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<CurrentUserResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("UserName and Password are required.");
        }

        var user = await _userManager.FindByNameAsync(request.UserName.Trim());

        if (user is null)
        {
            return Unauthorized();
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return Unauthorized();
        }

        await _signInManager.SignInAsync(user, isPersistent: true);

        return Ok(await MapCurrentUserAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await MapCurrentUserAsync(user));
    }

    private async Task<CurrentUserResponse> MapCurrentUserAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new CurrentUserResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            DisplayName = string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.UserName ?? string.Empty
                : user.DisplayName,
            Roles = roles.OrderBy(x => x).ToList()
        };
    }
}
