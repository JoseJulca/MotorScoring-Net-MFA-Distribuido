using MotorScoring.Identity.Contracts;
using MotorScoring.Identity.Domain.Entities;

namespace MotorScoring.Identity.Application.Ports.Out;

public interface ITokenPort
{
    Task<TokenResponse> CreateTokensAsync(UserAccount user, CancellationToken ct = default);
    Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
    string CreateMfaChallenge(UserAccount user);
    Guid? ValidateMfaChallenge(string token);
    string CreateExternalExchangeCode(UserAccount user);
    Guid? ValidateExternalExchangeCode(string token);
    Task<UserInfoResponse> BuildUserInfoAsync(UserAccount user, CancellationToken ct = default);
}
