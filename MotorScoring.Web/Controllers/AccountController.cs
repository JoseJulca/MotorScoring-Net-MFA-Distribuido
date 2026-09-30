using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorScoring.Web.Models.Auth;
using MotorScoring.Web.Services;

namespace MotorScoring.Web.Controllers;

public sealed class AccountController(
    IIdentityApiService identity,
    WebSignInService signIn,
    TokenSessionService tokenSession,
    QrCodeService qrCodeService,
    IConfiguration configuration) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Crear", "SolicitudesCredito");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var response = await identity.LoginAsync(model.Email, model.Password, ct);
            return await CompleteLoginAsync(response, model.ReturnUrl);
        }
        catch (ApiException)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña inválidos, o la cuenta no está disponible.");
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ExternalLogin(string provider)
    {
        if (provider is not ("Google" or "GitHub")) return BadRequest();
        var identityPublic = (configuration["Identity:PublicBaseUrl"] ?? "http://localhost:8082").TrimEnd('/');
        return Redirect($"{identityPublic}/api/auth/external/{provider}");
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ExternalCallback(string? code, string? error, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            TempData["AuthError"] = error;
            return RedirectToAction(nameof(Login));
        }
        if (string.IsNullOrWhiteSpace(code)) return RedirectToAction(nameof(Login));
        try
        {
            var response = await identity.ExchangeExternalAsync(code, ct);
            return await CompleteLoginAsync(response, null);
        }
        catch (ApiException)
        {
            TempData["AuthError"] = "No se pudo completar el inicio de sesión externo.";
            return RedirectToAction(nameof(Login));
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Mfa(string challengeToken, string? returnUrl = null) =>
        View(new MfaViewModel { ChallengeToken = challengeToken, ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mfa(MfaViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var response = await identity.VerifyMfaAsync(model.ChallengeToken, model.Code, ct);
            return await CompleteLoginAsync(response, model.ReturnUrl);
        }
        catch (ApiException)
        {
            ModelState.AddModelError(string.Empty, "El código MFA no es válido o expiró.");
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult RecoveryCode(string challengeToken, string? returnUrl = null) =>
        View(new RecoveryCodeViewModel { ChallengeToken = challengeToken, ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecoveryCode(RecoveryCodeViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var response = await identity.VerifyRecoveryCodeAsync(model.ChallengeToken, model.RecoveryCode, ct);
            return await CompleteLoginAsync(response, model.ReturnUrl);
        }
        catch (ApiException)
        {
            ModelState.AddModelError(string.Empty, "El código de recuperación no es válido.");
            return View(model);
        }
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MfaSetup(CancellationToken ct)
    {
        var token = await tokenSession.GetValidAccessTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token)) return Challenge();
        var setup = await identity.SetupMfaAsync(token, ct);
        return View(CreateMfaSetupViewModel(setup));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MfaSetup(MfaSetupViewModel model, CancellationToken ct)
    {
        var token = await tokenSession.GetValidAccessTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token)) return Challenge();
        if (!ModelState.IsValid)
        {
            var setup = await identity.SetupMfaAsync(token, ct);
            PopulateMfaSetup(model, setup);
            return View(model);
        }
        try
        {
            var enabled = await identity.EnableMfaAsync(token, model.Code, ct);
            model.Enabled = enabled.Enabled;
            model.RecoveryCodes = enabled.RecoveryCodes;
            return View(model);
        }
        catch (ApiException)
        {
            ModelState.AddModelError(string.Empty, "El código no corresponde a la clave configurada.");
            var setup = await identity.SetupMfaAsync(token, ct);
            PopulateMfaSetup(model, setup);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refresh = await HttpContext.GetTokenAsync("refresh_token");
        if (!string.IsNullOrWhiteSpace(refresh))
        {
            try { await identity.RevokeAsync(refresh, ct); } catch { }
        }
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private MfaSetupViewModel CreateMfaSetupViewModel(IdentityMfaSetupResponse setup)
    {
        var model = new MfaSetupViewModel();
        PopulateMfaSetup(model, setup);
        return model;
    }

    private void PopulateMfaSetup(MfaSetupViewModel model, IdentityMfaSetupResponse setup)
    {
        model.SharedKey = setup.SharedKey;
        model.AuthenticatorUri = setup.AuthenticatorUri;
        model.QrCodeBase64 = qrCodeService.GenerateBase64Png(setup.AuthenticatorUri);
    }

    private async Task<IActionResult> CompleteLoginAsync(IdentityLoginResponse response, string? returnUrl)
    {
        if (response.RequiresMfa && !string.IsNullOrWhiteSpace(response.MfaChallengeToken))
            return RedirectToAction(nameof(Mfa), new { challengeToken = response.MfaChallengeToken, returnUrl });
        if (!response.Succeeded || response.Tokens is null)
        {
            TempData["AuthError"] = response.Message ?? "No se pudo iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }
        await signIn.SignInAsync(response.Tokens);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction("Crear", "SolicitudesCredito");
    }
}
