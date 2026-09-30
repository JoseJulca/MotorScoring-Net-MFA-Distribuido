using System.ComponentModel.DataAnnotations;

namespace MotorScoring.Identity.Contracts;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(10)] string Password,
    string? DisplayName);

public sealed record MfaVerifyRequest(
    [Required] string ChallengeToken,
    [Required] string Code);

public sealed record RecoveryCodeVerifyRequest(
    [Required] string ChallengeToken,
    [Required] string RecoveryCode);

public sealed record RefreshRequest([Required] string RefreshToken);
public sealed record RevokeRequest([Required] string RefreshToken);
public sealed record ExternalExchangeRequest([Required] string Code);
public sealed record MfaEnableRequest([Required] string Code);

public sealed record UserInfoResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    bool MfaEnabled,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserInfoResponse User);

public sealed record LoginResponse(
    bool Succeeded,
    bool RequiresMfa,
    string? MfaChallengeToken,
    string? Message,
    TokenResponse? Tokens);

public sealed record MfaSetupResponse(string SharedKey, string AuthenticatorUri);
public sealed record MfaEnableResponse(bool Enabled, IReadOnlyList<string> RecoveryCodes);
