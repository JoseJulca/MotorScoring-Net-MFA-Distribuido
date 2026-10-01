using MotorScoring.Identity.Domain.Entities;

namespace MotorScoring.Identity.Application.Ports.Out;

public interface IIdentityUserPort
{
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<UserAccount?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserAccount?> FindByExternalLoginAsync(string provider, string providerKey, CancellationToken ct = default);
    Task<(bool Succeeded, IReadOnlyList<string> Errors, UserAccount? User)> CreateLocalAsync(string email, string password, string? displayName, CancellationToken ct = default);
    Task<(bool Succeeded, IReadOnlyList<string> Errors, UserAccount? User)> CreateExternalAsync(string email, string? displayName, CancellationToken ct = default);
    Task<(bool Succeeded, IReadOnlyList<string> Errors)> AddExternalLoginAsync(Guid userId, string provider, string providerKey, CancellationToken ct = default);
    Task<bool> IsLockedOutAsync(Guid userId, CancellationToken ct = default);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct = default);
    Task AccessFailedAsync(Guid userId, CancellationToken ct = default);
    Task ResetAccessFailedCountAsync(Guid userId, CancellationToken ct = default);
    Task<bool> VerifyAuthenticatorCodeAsync(Guid userId, string code, CancellationToken ct = default);
    Task<bool> RedeemRecoveryCodeAsync(Guid userId, string recoveryCode, CancellationToken ct = default);
    Task<string?> GetAuthenticatorKeyAsync(Guid userId, CancellationToken ct = default);
    Task<string?> ResetAuthenticatorKeyAsync(Guid userId, CancellationToken ct = default);
    Task SetTwoFactorEnabledAsync(Guid userId, bool enabled, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(Guid userId, int number, CancellationToken ct = default);
    Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken ct = default);
    Task<bool> RoleExistsAsync(string roleName, CancellationToken ct = default);
    Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
    Task<(bool Succeeded, IReadOnlyList<string> Errors)> AddToRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
    Task RemoveFromRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
}
