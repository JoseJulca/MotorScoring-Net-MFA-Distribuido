using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorScoring.Identity.Application.Models;
using MotorScoring.Identity.Application.Ports.In;

namespace MotorScoring.Identity.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Administrador")]
public sealed class UsersController(IUserAdministrationUseCase users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var list = await users.GetUsersAsync(ct);
        return Ok(list.Select(x => new { x.Id, x.Email, x.DisplayName, x.TwoFactorEnabled, x.LockoutEnd }).ToList());
    }

    [HttpPut("{id:guid}/roles/{roleName}")]
    public async Task<IActionResult> AddRole(Guid id, string roleName, CancellationToken ct)
    {
        var result = await users.AddRoleAsync(id, roleName, ct);
        if (result.Succeeded) return NoContent();
        if (result.Error == AuthError.NotFound) return NotFound();
        if (result.Message == "El rol no existe.") return BadRequest(new { message = result.Message });
        return BadRequest(result.Errors);
    }

    [HttpDelete("{id:guid}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(Guid id, string roleName, CancellationToken ct)
    {
        var result = await users.RemoveRoleAsync(id, roleName, ct);
        return result.Error == AuthError.NotFound ? NotFound() : NoContent();
    }
}
