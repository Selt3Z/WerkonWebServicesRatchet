using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Contracts.Auth;
using WerkonWebServicesRatchet.Infrastructure.Identity;

namespace WerkonWebServicesRatchet.Features.Auth;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthorizationPolicies.ManageUsers)]
public sealed class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserListItemResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .OrderBy(x => x.UserName)
            .ToListAsync(cancellationToken);

        var response = new List<UserListItemResponse>();

        foreach (var user in users)
        {
            response.Add(await MapListItemAsync(user));
        }

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserListItemResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(await MapListItemAsync(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserListItemResponse>> Create(
        SaveUserRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequestAsync(request, isEdit: false, cancellationToken);

        if (validationError is not null)
        {
            return BadRequest(new ApiErrorResponse { Message = validationError });
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Email = $"{request.UserName.Trim()}@local",
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password!);

        if (!createResult.Succeeded)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = string.Join(" ", createResult.Errors.Select(x => x.Description))
            });
        }

        await _userManager.AddToRoleAsync(user, request.Role.Trim());

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, await MapListItemAsync(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserListItemResponse>> Update(
        Guid id,
        SaveUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        var validationError = await ValidateRequestAsync(request, isEdit: true, cancellationToken);

        if (validationError is not null)
        {
            return BadRequest(new ApiErrorResponse { Message = validationError });
        }

        user.DisplayName = request.DisplayName.Trim();

        if (!string.Equals(user.UserName, request.UserName.Trim(), StringComparison.Ordinal))
        {
            var setUserNameResult = await _userManager.SetUserNameAsync(user, request.UserName.Trim());

            if (!setUserNameResult.Succeeded)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Message = string.Join(" ", setUserNameResult.Errors.Select(x => x.Description))
                });
            }

            await _userManager.SetEmailAsync(user, $"{request.UserName.Trim()}@local");
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);

            if (!passwordResult.Succeeded)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Message = string.Join(" ", passwordResult.Errors.Select(x => x.Description))
                });
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = string.Join(" ", updateResult.Errors.Select(x => x.Description))
            });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var targetRole = request.Role.Trim();

        if (!currentRoles.Contains(targetRole))
        {
            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await _userManager.AddToRoleAsync(user, targetRole);
        }

        return Ok(await MapListItemAsync(user));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser is not null && currentUser.Id == user.Id)
        {
            return BadRequest(new ApiErrorResponse { Message = "You cannot delete your own account." });
        }

        var deleteResult = await _userManager.DeleteAsync(user);

        if (!deleteResult.Succeeded)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = string.Join(" ", deleteResult.Errors.Select(x => x.Description))
            });
        }

        return NoContent();
    }

    private async Task<string?> ValidateRequestAsync(
        SaveUserRequest request,
        bool isEdit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return "UserName is required.";
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return "DisplayName is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Role) || !AppRoles.All.Contains(request.Role))
        {
            return "Role is required.";
        }

        if (!isEdit && string.IsNullOrWhiteSpace(request.Password))
        {
            return "Password is required.";
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            return "Role does not exist.";
        }

        return null;
    }

    private async Task<UserListItemResponse> MapListItemAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserListItemResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            DisplayName = user.DisplayName,
            Role = roles.OrderBy(x => x).FirstOrDefault() ?? string.Empty
        };
    }
}
