using Microsoft.AspNetCore.Authentication;
using MotorScoring.Web.Models.Auth;

namespace MotorScoring.Web.Services;

public sealed class TokenSessionService(
    IHttpContextAccessor accessor,
    IIdentityApiService identity,
    WebSignInService signIn)
{
    public async Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default)
    {
        var context = accessor.HttpContext;
        if (context?.User.Identity?.IsAuthenticated != true) return null;

        var access = await context.GetTokenAsync("access_token");
        var expiresRaw = await context.GetTokenAsync("expires_at");
        if (!string.IsNullOrWhiteSpace(access) && DateTimeOffset.TryParse(expiresRaw, out var expires) &&
            expires > DateTimeOffset.UtcNow.AddSeconds(30)) return access;

        var refresh = await context.GetTokenAsync("refresh_token");
        if (string.IsNullOrWhiteSpace(refresh)) return null;
        try
        {
            var tokens = await identity.RefreshAsync(refresh, ct);
            await signIn.SignInAsync(tokens);
            return tokens.AccessToken;
        }
        catch
        {
            return null;
        }
    }
}
