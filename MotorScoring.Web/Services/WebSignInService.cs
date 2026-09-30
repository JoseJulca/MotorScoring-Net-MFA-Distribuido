using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MotorScoring.Web.Models.Auth;

namespace MotorScoring.Web.Services;

public sealed class WebSignInService(IHttpContextAccessor accessor)
{
    public async Task SignInAsync(IdentityTokens tokens)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, tokens.User.Id.ToString()),
            new(ClaimTypes.Email, tokens.User.Email),
            new(ClaimTypes.Name, tokens.User.DisplayName ?? tokens.User.Email),
            new("mfa", tokens.User.MfaEnabled ? "true" : "false")
        };
        claims.AddRange(tokens.User.Roles.Select(x => new Claim(ClaimTypes.Role, x)));
        claims.AddRange(tokens.User.Permissions.Select(x => new Claim("permission", x)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        var properties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = tokens.RefreshTokenExpiresAt
        };
        properties.StoreTokens([
            new AuthenticationToken { Name = "access_token", Value = tokens.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = tokens.RefreshToken },
            new AuthenticationToken { Name = "expires_at", Value = tokens.AccessTokenExpiresAt.ToString("O") }
        ]);
        await accessor.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }
}
