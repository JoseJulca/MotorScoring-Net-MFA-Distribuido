using MotorScoring.Web.Models.Auth;

namespace MotorScoring.Web.Services;

public interface IIdentityApiService
{
    Task<IdentityLoginResponse> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<IdentityLoginResponse> VerifyRecoveryCodeAsync(string challengeToken, string recoveryCode, CancellationToken ct = default);
    Task<IdentityLoginResponse> VerifyMfaAsync(string challengeToken, string code, CancellationToken ct = default);
    Task<IdentityLoginResponse> ExchangeExternalAsync(string code, CancellationToken ct = default);
    Task<IdentityTokens> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<IdentityMfaSetupResponse> SetupMfaAsync(string accessToken, CancellationToken ct = default);
    Task<IdentityMfaEnableResponse> EnableMfaAsync(string accessToken, string code, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
}
