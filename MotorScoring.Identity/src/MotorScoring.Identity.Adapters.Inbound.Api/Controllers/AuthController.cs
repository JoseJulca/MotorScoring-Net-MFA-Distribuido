using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MotorScoring.Identity.Application.Models;
using MotorScoring.Identity.Application.Ports.In;
using MotorScoring.Identity.Contracts;

namespace MotorScoring.Identity.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthUseCase authUseCase, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authUseCase.RegisterAsync(request, configuration.GetValue("Security:AllowSelfRegistration", false), ct);
        if (result.Succeeded && result.Value is not null) return Created("/api/auth/me", new { result.Value.Id, result.Value.Email, result.Value.DisplayName });
        return result.Error switch
        {
            AuthError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
            AuthError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message, errors = result.Errors })
        };
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) => ToLoginResult(await authUseCase.LoginAsync(request, ct));

    [AllowAnonymous]
    [HttpPost("mfa/verify")]
    public async Task<ActionResult<LoginResponse>> VerifyMfa(MfaVerifyRequest request, CancellationToken ct) => ToLoginResult(await authUseCase.VerifyMfaAsync(request, ct));

    [AllowAnonymous]
    [HttpPost("mfa/recovery")]
    public async Task<ActionResult<LoginResponse>> VerifyRecoveryCode(RecoveryCodeVerifyRequest request, CancellationToken ct) => ToLoginResult(await authUseCase.VerifyRecoveryCodeAsync(request, ct));

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var tokens = await authUseCase.RefreshAsync(request.RefreshToken, ct);
        return tokens is null ? Unauthorized(new { message = "Refresh token inválido o expirado." }) : Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RevokeRequest request, CancellationToken ct)
    {
        await authUseCase.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        var info = await authUseCase.GetUserInfoAsync(id.Value, ct);
        return info is null ? Unauthorized() : Ok(info);
    }

    [Authorize]
    [HttpPost("mfa/setup")]
    public async Task<IActionResult> SetupMfa(CancellationToken ct)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        var result = await authUseCase.SetupMfaAsync(id.Value, configuration["Mfa:Issuer"] ?? "MotorScoring", ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [Authorize]
    [HttpPost("mfa/enable")]
    public async Task<IActionResult> EnableMfa(MfaEnableRequest request, CancellationToken ct)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        var result = await authUseCase.EnableMfaAsync(id.Value, request.Code, ct);
        return result.Succeeded ? Ok(result.Value) : result.Error == AuthError.Unauthorized ? Unauthorized() : BadRequest(new { message = result.Message });
    }

    [Authorize]
    [HttpPost("mfa/disable")]
    public async Task<IActionResult> DisableMfa(CancellationToken ct)
    {
        var id = CurrentUserId();
        if (id is null || !await authUseCase.DisableMfaAsync(id.Value, ct)) return Unauthorized();
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("external/{provider}")]
    public IActionResult External(string provider)
    {
        if (!IsExternalProviderEnabled(provider)) return BadRequest(new { message = $"El proveedor {provider} no está configurado." });
        var callback = Url.ActionLink(nameof(ExternalCallback), values: new { provider })!;
        return Challenge(new AuthenticationProperties { RedirectUri = callback }, provider);
    }

    [AllowAnonymous]
    [HttpGet("external/callback")]
    public async Task<IActionResult> ExternalCallback(string provider, CancellationToken ct)
    {
        var auth = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!auth.Succeeded || auth.Principal is null) return Redirect(BuildWebCallback(error: "No se pudo completar la autenticación externa."));
        var providerKey = auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(providerKey)) return Redirect(BuildWebCallback(error: "El proveedor no devolvió un identificador de usuario."));

        var result = await authUseCase.CompleteExternalLoginAsync(new ExternalUserInput(provider, providerKey, auth.Principal.FindFirstValue(ClaimTypes.Email), auth.Principal.FindFirstValue(ClaimTypes.Name)), ct);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return result.Succeeded && result.Value is not null
            ? Redirect(BuildWebCallback(code: result.Value.ExchangeCode))
            : Redirect(BuildWebCallback(error: result.Message ?? "Error de autenticación externa"));
    }

    [AllowAnonymous]
    [HttpPost("external/exchange")]
    public async Task<ActionResult<LoginResponse>> ExchangeExternal(ExternalExchangeRequest request, CancellationToken ct) => ToLoginResult(await authUseCase.ExchangeExternalAsync(request.Code, ct));

    private ActionResult<LoginResponse> ToLoginResult(OperationResult<LoginResponse> result) =>
        result.Succeeded && result.Value is not null ? Ok(result.Value) : Unauthorized(new LoginResponse(false, false, null, result.Message, null));

    private Guid? CurrentUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(id, out var parsed) ? parsed : null;
    }

    private bool IsExternalProviderEnabled(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]),
        "github" => !string.IsNullOrWhiteSpace(configuration["Authentication:GitHub:ClientId"]),
        _ => false
    };

    private string BuildWebCallback(string? code = null, string? error = null)
    {
        var webBase = (configuration["WebClient:BaseUrl"] ?? "http://localhost:8081").TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(code)) return $"{webBase}/Account/ExternalCallback?code={Uri.EscapeDataString(code)}";
        return $"{webBase}/Account/ExternalCallback?error={Uri.EscapeDataString(error ?? "Error de autenticación externa")}";
    }
}
