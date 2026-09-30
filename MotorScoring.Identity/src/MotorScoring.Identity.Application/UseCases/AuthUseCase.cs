using MotorScoring.Identity.Application.Models;
using MotorScoring.Identity.Application.Ports.In;
using MotorScoring.Identity.Application.Ports.Out;
using MotorScoring.Identity.Contracts;

namespace MotorScoring.Identity.Application.UseCases;

public sealed class AuthUseCase(IIdentityUserPort users, ITokenPort tokens) : IAuthUseCase
{
    public async Task<OperationResult<RegisteredUser>> RegisterAsync(RegisterRequest request, bool allowSelfRegistration, CancellationToken ct = default)
    {
        if (!allowSelfRegistration)
            return OperationResult<RegisteredUser>.Fail(AuthError.Forbidden, "El auto-registro está deshabilitado.");

        var email = request.Email.Trim();
        if (await users.FindByEmailAsync(email, ct) is not null)
            return OperationResult<RegisteredUser>.Fail(AuthError.Conflict, "El correo ya se encuentra registrado.");

        var created = await users.CreateLocalAsync(email, request.Password, request.DisplayName?.Trim(), ct);
        if (!created.Succeeded || created.User is null)
            return OperationResult<RegisteredUser>.Fail(AuthError.BadRequest, "No se pudo crear el usuario.", created.Errors);

        return OperationResult<RegisteredUser>.Ok(new RegisteredUser(created.User.Id, created.User.Email, created.User.DisplayName));
    }

    public async Task<OperationResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim(), ct);
        if (user is null)
            return Unauthorized("Credenciales inválidas.");
        if (await users.IsLockedOutAsync(user.Id, ct))
            return Unauthorized("La cuenta está temporalmente bloqueada.");
        if (!await users.CheckPasswordAsync(user.Id, request.Password, ct))
        {
            await users.AccessFailedAsync(user.Id, ct);
            return Unauthorized("Credenciales inválidas.");
        }

        await users.ResetAccessFailedCountAsync(user.Id, ct);
        if (user.MfaEnabled)
            return OperationResult<LoginResponse>.Ok(new LoginResponse(false, true, tokens.CreateMfaChallenge(user), "Se requiere MFA.", null));

        return OperationResult<LoginResponse>.Ok(new LoginResponse(true, false, null, null, await tokens.CreateTokensAsync(user, ct)));
    }

    public async Task<OperationResult<LoginResponse>> VerifyMfaAsync(MfaVerifyRequest request, CancellationToken ct = default)
    {
        var userId = tokens.ValidateMfaChallenge(request.ChallengeToken);
        if (userId is null) return Unauthorized("El desafío MFA no es válido o expiró.");
        var user = await users.FindByIdAsync(userId.Value, ct);
        if (user is null || !user.MfaEnabled) return Unauthorized("No se pudo validar MFA.");

        var code = NormalizeCode(request.Code);
        if (!await users.VerifyAuthenticatorCodeAsync(user.Id, code, ct))
        {
            await users.AccessFailedAsync(user.Id, ct);
            return Unauthorized("Código MFA inválido.");
        }

        await users.ResetAccessFailedCountAsync(user.Id, ct);
        return OperationResult<LoginResponse>.Ok(new LoginResponse(true, false, null, null, await tokens.CreateTokensAsync(user, ct)));
    }

    public async Task<OperationResult<LoginResponse>> VerifyRecoveryCodeAsync(RecoveryCodeVerifyRequest request, CancellationToken ct = default)
    {
        var userId = tokens.ValidateMfaChallenge(request.ChallengeToken);
        if (userId is null) return Unauthorized("El desafío MFA no es válido o expiró.");
        var user = await users.FindByIdAsync(userId.Value, ct);
        if (user is null || !user.MfaEnabled) return Unauthorized("No se pudo validar MFA.");
        if (!await users.RedeemRecoveryCodeAsync(user.Id, request.RecoveryCode.Trim(), ct))
            return Unauthorized("Código de recuperación inválido.");
        return OperationResult<LoginResponse>.Ok(new LoginResponse(true, false, null, null, await tokens.CreateTokensAsync(user, ct)));
    }

    public Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default) => tokens.RefreshAsync(refreshToken, ct);
    public Task RevokeAsync(string refreshToken, CancellationToken ct = default) => tokens.RevokeAsync(refreshToken, ct);

    public async Task<UserInfoResponse?> GetUserInfoAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId, ct);
        return user is null ? null : await tokens.BuildUserInfoAsync(user, ct);
    }

    public async Task<MfaSetupResponse?> SetupMfaAsync(Guid userId, string issuer, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId, ct);
        if (user is null) return null;
        var key = await users.GetAuthenticatorKeyAsync(userId, ct);
        if (string.IsNullOrWhiteSpace(key)) key = await users.ResetAuthenticatorKeyAsync(userId, ct);
        if (string.IsNullOrWhiteSpace(key)) return null;
        var escapedIssuer = Uri.EscapeDataString(issuer);
        var account = Uri.EscapeDataString(user.Email);
        return new MfaSetupResponse(key, $"otpauth://totp/{escapedIssuer}:{account}?secret={key}&issuer={escapedIssuer}&digits=6");
    }

    public async Task<OperationResult<MfaEnableResponse>> EnableMfaAsync(Guid userId, string code, CancellationToken ct = default)
    {
        if (await users.FindByIdAsync(userId, ct) is null)
            return OperationResult<MfaEnableResponse>.Fail(AuthError.Unauthorized, "Usuario no encontrado.");
        if (!await users.VerifyAuthenticatorCodeAsync(userId, NormalizeCode(code), ct))
            return OperationResult<MfaEnableResponse>.Fail(AuthError.BadRequest, "El código del autenticador no es válido.");
        await users.SetTwoFactorEnabledAsync(userId, true, ct);
        var recovery = await users.GenerateRecoveryCodesAsync(userId, 8, ct);
        return OperationResult<MfaEnableResponse>.Ok(new MfaEnableResponse(true, recovery));
    }

    public async Task<bool> DisableMfaAsync(Guid userId, CancellationToken ct = default)
    {
        if (await users.FindByIdAsync(userId, ct) is null) return false;
        await users.SetTwoFactorEnabledAsync(userId, false, ct);
        await users.ResetAuthenticatorKeyAsync(userId, ct);
        return true;
    }

    public async Task<OperationResult<ExternalLoginResult>> CompleteExternalLoginAsync(ExternalUserInput input, CancellationToken ct = default)
    {
        var user = await users.FindByExternalLoginAsync(input.Provider, input.ProviderKey, ct);
        if (user is null && !string.IsNullOrWhiteSpace(input.Email))
            user = await users.FindByEmailAsync(input.Email, ct);

        if (user is null)
        {
            var email = input.Email ?? $"{input.Provider.ToLowerInvariant()}-{input.ProviderKey}@external.motorscoring.local";

            var created = await users.CreateExternalAsync(
                email,
                input.DisplayName,
                ct);

            if (!created.Succeeded || created.User is null)
                return OperationResult<ExternalLoginResult>.Fail(
                    AuthError.BadRequest,
                    "No se pudo crear el usuario externo.",
                    created.Errors);

            user = created.User;

            // Todo usuario creado mediante un proveedor externo
            // inicia con el rol Analista.
            const string defaultRole = "Analista";

            if (!await users.RoleExistsAsync(defaultRole, ct))
                return OperationResult<ExternalLoginResult>.Fail(
                    AuthError.BadRequest,
                    $"El rol {defaultRole} no existe.");

            var roleResult = await users.AddToRoleAsync(
                user.Id,
                defaultRole,
                ct);

            if (!roleResult.Succeeded)
                return OperationResult<ExternalLoginResult>.Fail(
                    AuthError.BadRequest,
                    $"No se pudo asignar el rol {defaultRole}.",
                    roleResult.Errors);
        }

        var existing = await users.FindByExternalLoginAsync(input.Provider, input.ProviderKey, ct);
        if (existing is null)
        {
            var link = await users.AddExternalLoginAsync(user.Id, input.Provider, input.ProviderKey, ct);
            if (!link.Succeeded)
                return OperationResult<ExternalLoginResult>.Fail(AuthError.BadRequest, "No se pudo vincular el proveedor externo.", link.Errors);
        }

        return OperationResult<ExternalLoginResult>.Ok(new ExternalLoginResult(user, tokens.CreateExternalExchangeCode(user)));
    }

    public async Task<OperationResult<LoginResponse>> ExchangeExternalAsync(string code, CancellationToken ct = default)
    {
        var id = tokens.ValidateExternalExchangeCode(code);
        if (id is null) return Unauthorized("El código externo es inválido o expiró.");
        var user = await users.FindByIdAsync(id.Value, ct);
        if (user is null) return Unauthorized("Usuario no encontrado.");
        if (user.MfaEnabled)
            return OperationResult<LoginResponse>.Ok(new LoginResponse(false, true, tokens.CreateMfaChallenge(user), "Se requiere MFA.", null));
        return OperationResult<LoginResponse>.Ok(new LoginResponse(true, false, null, null, await tokens.CreateTokensAsync(user, ct)));
    }

    private static OperationResult<LoginResponse> Unauthorized(string message) =>
        OperationResult<LoginResponse>.Fail(AuthError.Unauthorized, message);

    private static string NormalizeCode(string code) => code.Replace(" ", string.Empty).Replace("-", string.Empty);
}
