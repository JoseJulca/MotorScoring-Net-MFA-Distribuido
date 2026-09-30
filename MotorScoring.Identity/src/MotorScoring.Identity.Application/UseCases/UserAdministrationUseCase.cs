using MotorScoring.Identity.Application.Models;
using MotorScoring.Identity.Application.Ports.In;
using MotorScoring.Identity.Application.Ports.Out;
using MotorScoring.Identity.Domain.Entities;

namespace MotorScoring.Identity.Application.UseCases;

public sealed class UserAdministrationUseCase(IIdentityUserPort users) : IUserAdministrationUseCase
{
    public Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken ct = default) => users.GetUsersAsync(ct);

    public async Task<OperationResult<bool>> AddRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        if (await users.FindByIdAsync(userId, ct) is null)
            return OperationResult<bool>.Fail(AuthError.NotFound, "Usuario no encontrado.");
        if (!await users.RoleExistsAsync(roleName, ct))
            return OperationResult<bool>.Fail(AuthError.BadRequest, "El rol no existe.");
        if (!await users.IsInRoleAsync(userId, roleName, ct))
        {
            var result = await users.AddToRoleAsync(userId, roleName, ct);
            if (!result.Succeeded)
                return OperationResult<bool>.Fail(AuthError.BadRequest, "No se pudo asignar el rol.", result.Errors);
        }
        return OperationResult<bool>.Ok(true);
    }

    public async Task<OperationResult<bool>> RemoveRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        if (await users.FindByIdAsync(userId, ct) is null)
            return OperationResult<bool>.Fail(AuthError.NotFound, "Usuario no encontrado.");
        if (await users.IsInRoleAsync(userId, roleName, ct))
            await users.RemoveFromRoleAsync(userId, roleName, ct);
        return OperationResult<bool>.Ok(true);
    }
}
