using System.ComponentModel.DataAnnotations;

namespace MotorScoring.Web.Models.Auth;

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public sealed class MfaViewModel
{
    [Required] public string ChallengeToken { get; set; } = string.Empty;
    [Required, StringLength(8, MinimumLength = 6)]
    [Display(Name = "Código del autenticador")]
    public string Code { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public sealed class RecoveryCodeViewModel
{
    [Required] public string ChallengeToken { get; set; } = string.Empty;
    [Required, Display(Name = "Código de recuperación")] public string RecoveryCode { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public sealed class MfaSetupViewModel
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
    public string QrCodeBase64 { get; set; } = string.Empty;
    [Required, StringLength(8, MinimumLength = 6)]
    [Display(Name = "Código de verificación")]
    public string Code { get; set; } = string.Empty;
    public IReadOnlyList<string> RecoveryCodes { get; set; } = Array.Empty<string>();
    public bool Enabled { get; set; }
}

public sealed record IdentityUserInfo(Guid Id, string Email, string? DisplayName, bool MfaEnabled, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
public sealed record IdentityTokens(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt, IdentityUserInfo User);
public sealed record IdentityLoginResponse(bool Succeeded, bool RequiresMfa, string? MfaChallengeToken, string? Message, IdentityTokens? Tokens);
public sealed record IdentityMfaSetupResponse(string SharedKey, string AuthenticatorUri);
public sealed record IdentityMfaEnableResponse(bool Enabled, IReadOnlyList<string> RecoveryCodes);
