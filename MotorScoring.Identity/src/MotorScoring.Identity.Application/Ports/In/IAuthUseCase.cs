using MotorScoring.Identity.Application.Models;
using MotorScoring.Identity.Contracts;

namespace MotorScoring.Identity.Application.Ports.In;

public interface IAuthUseCase
{
    Task<OperationResult<RegisteredUser>> RegisterAsync(RegisterRequest request, bool allowSelfRegistration, CancellationToken ct = default);
    Task<OperationResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<OperationResult<LoginResponse>> VerifyMfaAsync(MfaVerifyRequest request, CancellationToken ct = default);
    Task<OperationResult<LoginResponse>> VerifyRecoveryCodeAsync(RecoveryCodeVerifyRequest request, CancellationToken ct = default);
    Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
    Task<UserInfoResponse?> GetUserInfoAsync(Guid userId, CancellationToken ct = default);
    Task<MfaSetupResponse?> SetupMfaAsync(Guid userId, string issuer, CancellationToken ct = default);
    Task<OperationResult<MfaEnableResponse>> EnableMfaAsync(Guid userId, string code, CancellationToken ct = default);
    Task<bool> DisableMfaAsync(Guid userId, CancellationToken ct = default);
    Task<OperationResult<ExternalLoginResult>> CompleteExternalLoginAsync(ExternalUserInput input, CancellationToken ct = default);
    Task<OperationResult<LoginResponse>> ExchangeExternalAsync(string code, CancellationToken ct = default);
}
