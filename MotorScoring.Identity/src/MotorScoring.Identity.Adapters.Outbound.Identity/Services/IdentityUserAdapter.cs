using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotorScoring.Identity.Application.Ports.Out;
using MotorScoring.Identity.Domain.Entities;
using MotorScoring.Identity.Models;

namespace MotorScoring.Identity.Services;

public sealed class IdentityUserAdapter(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IIdentityUserPort
{
    public async Task<UserAccount?> FindByEmailAsync(string email, CancellationToken ct = default) => Map(await userManager.FindByEmailAsync(email));
    public async Task<UserAccount?> FindByIdAsync(Guid id, CancellationToken ct = default) => Map(await userManager.FindByIdAsync(id.ToString()));
    public async Task<UserAccount?> FindByExternalLoginAsync(string provider, string providerKey, CancellationToken ct = default) => Map(await userManager.FindByLoginAsync(provider, providerKey));

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors, UserAccount? User)> CreateLocalAsync(string email, string password, string? displayName, CancellationToken ct = default)
    {
        var user = NewUser(email, displayName);
        var result = await userManager.CreateAsync(user, password);
        return (result.Succeeded, result.Errors.Select(x => x.Description).ToArray(), result.Succeeded ? Map(user) : null);
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors, UserAccount? User)> CreateExternalAsync(string email, string? displayName, CancellationToken ct = default)
    {
        var user = NewUser(email, displayName);
        var result = await userManager.CreateAsync(user);
        return (result.Succeeded, result.Errors.Select(x => x.Description).ToArray(), result.Succeeded ? Map(user) : null);
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors)> AddExternalLoginAsync(Guid userId, string provider, string providerKey, CancellationToken ct = default)
    {
        var user = await RequireUser(userId);
        var result = await userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
        return (result.Succeeded, result.Errors.Select(x => x.Description).ToArray());
    }

    public async Task<bool> IsLockedOutAsync(Guid userId, CancellationToken ct = default) => await userManager.IsLockedOutAsync(await RequireUser(userId));
    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct = default) => await userManager.CheckPasswordAsync(await RequireUser(userId), password);
    public async Task AccessFailedAsync(Guid userId, CancellationToken ct = default) => _ = await userManager.AccessFailedAsync(await RequireUser(userId));
    public async Task ResetAccessFailedCountAsync(Guid userId, CancellationToken ct = default) => _ = await userManager.ResetAccessFailedCountAsync(await RequireUser(userId));
    public async Task<bool> VerifyAuthenticatorCodeAsync(Guid userId, string code, CancellationToken ct = default) => await userManager.VerifyTwoFactorTokenAsync(await RequireUser(userId), TokenOptions.DefaultAuthenticatorProvider, code);
    public async Task<bool> RedeemRecoveryCodeAsync(Guid userId, string recoveryCode, CancellationToken ct = default) => (await userManager.RedeemTwoFactorRecoveryCodeAsync(await RequireUser(userId), recoveryCode)).Succeeded;
    public async Task<string?> GetAuthenticatorKeyAsync(Guid userId, CancellationToken ct = default) => await userManager.GetAuthenticatorKeyAsync(await RequireUser(userId));
    public async Task<string?> ResetAuthenticatorKeyAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await RequireUser(userId);
        await userManager.ResetAuthenticatorKeyAsync(user);
        return await userManager.GetAuthenticatorKeyAsync(user);
    }
    public async Task SetTwoFactorEnabledAsync(Guid userId, bool enabled, CancellationToken ct = default) => _ = await userManager.SetTwoFactorEnabledAsync(await RequireUser(userId), enabled);
    public async Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(Guid userId, int number, CancellationToken ct = default) => (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(await RequireUser(userId), number))?.ToArray() ?? [];

    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken ct = default) => await userManager.Users
        .Select(x => new UserSummary(x.Id, x.Email, x.DisplayName, x.TwoFactorEnabled, x.LockoutEnd))
        .ToListAsync(ct);

    public Task<bool> RoleExistsAsync(string roleName, CancellationToken ct = default) => roleManager.RoleExistsAsync(roleName);
    public async Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken ct = default) => await userManager.IsInRoleAsync(await RequireUser(userId), roleName);
    public async Task<(bool Succeeded, IReadOnlyList<string> Errors)> AddToRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        var result = await userManager.AddToRoleAsync(await RequireUser(userId), roleName);
        return (result.Succeeded, result.Errors.Select(x => x.Description).ToArray());
    }
    public async Task RemoveFromRoleAsync(Guid userId, string roleName, CancellationToken ct = default) => _ = await userManager.RemoveFromRoleAsync(await RequireUser(userId), roleName);

    private async Task<ApplicationUser> RequireUser(Guid id) => await userManager.FindByIdAsync(id.ToString()) ?? throw new InvalidOperationException("Usuario no encontrado.");
    private static UserAccount? Map(ApplicationUser? user) => user is null ? null : new UserAccount(user.Id, user.Email ?? string.Empty, user.DisplayName, user.TwoFactorEnabled, user.LockoutEnd);
    private static ApplicationUser NewUser(string email, string? displayName) => new()
    {
        Id = Guid.NewGuid(),
        UserName = email,
        Email = email,
        EmailConfirmed = true,
        DisplayName = displayName,
        LockoutEnabled = true
    };
}
