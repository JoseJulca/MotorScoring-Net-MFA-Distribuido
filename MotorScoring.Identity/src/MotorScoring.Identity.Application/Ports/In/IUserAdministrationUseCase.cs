using MotorScoring.Identity.Application.Models;
using MotorScoring.Identity.Domain.Entities;

namespace MotorScoring.Identity.Application.Ports.In;

public interface IUserAdministrationUseCase
{
    Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken ct = default);
    Task<OperationResult<bool>> AddRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
    Task<OperationResult<bool>> RemoveRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
}
