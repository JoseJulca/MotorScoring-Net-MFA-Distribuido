using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MotorScoring.Identity.Application.Ports.Out;
using MotorScoring.Identity.Contracts;
using MotorScoring.Identity.Data;
using MotorScoring.Identity.Domain.Entities;
using MotorScoring.Identity.Models;
using MotorScoring.Identity.Security;

namespace MotorScoring.Identity.Services;

public sealed class TokenService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IdentityDbContext db,
    IOptions<JwtOptions> options) : ITokenPort
{
    private readonly JwtOptions _options = options.Value;

    public async Task<TokenResponse> CreateTokensAsync(UserAccount user, CancellationToken ct = default)
    {
        var appUser = await RequireUser(user.Id);
        var info = await BuildUserInfoAsync(user, ct);
        var now = DateTimeOffset.UtcNow;
        var accessExpires = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email),
            new(SecurityConstants.MfaClaim, user.MfaEnabled ? "true" : "false")
        };
        foreach (var role in info.Roles) claims.Add(new Claim(ClaimTypes.Role, role));
        foreach (var permission in info.Permissions) claims.Add(new Claim(SecurityConstants.PermissionClaim, permission));

        var jwt = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, now.UtcDateTime, accessExpires.UtcDateTime,
            new SigningCredentials(GetKey(), SecurityAlgorithms.HmacSha256));
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        var refreshRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshExpires = now.AddDays(_options.RefreshTokenDays);
        db.RefreshTokens.Add(new RefreshToken { UserId = appUser.Id, TokenHash = Hash(refreshRaw), ExpiresAt = refreshExpires });
        await db.SaveChangesAsync(ct);
        return new TokenResponse(accessToken, accessExpires, refreshRaw, refreshExpires, info);
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var existing = await db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == Hash(refreshToken), ct);
        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= DateTimeOffset.UtcNow) return null;
        existing.RevokedAt = DateTimeOffset.UtcNow;
        var account = Map(existing.User);
        var replacement = await CreateTokensAsync(account, ct);
        existing.ReplacedByTokenHash = Hash(replacement.RefreshToken);
        await db.SaveChangesAsync(ct);
        return replacement;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var existing = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == Hash(refreshToken), ct);
        if (existing is null || existing.RevokedAt is not null) return;
        existing.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public string CreateMfaChallenge(UserAccount user) => CreatePurposeToken(user, "mfa", 5);
    public Guid? ValidateMfaChallenge(string token) => ValidatePurposeToken(token, "mfa");
    public string CreateExternalExchangeCode(UserAccount user) => CreatePurposeToken(user, "external_exchange", 2);
    public Guid? ValidateExternalExchangeCode(string token) => ValidatePurposeToken(token, "external_exchange");

    public async Task<UserInfoResponse> BuildUserInfoAsync(UserAccount user, CancellationToken ct = default)
    {
        var appUser = await RequireUser(user.Id);
        var roles = await userManager.GetRolesAsync(appUser);
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in await userManager.GetClaimsAsync(appUser))
            if (claim.Type == SecurityConstants.PermissionClaim) permissions.Add(claim.Value);
        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;
            foreach (var claim in await roleManager.GetClaimsAsync(role))
                if (claim.Type == SecurityConstants.PermissionClaim) permissions.Add(claim.Value);
        }
        return new UserInfoResponse(user.Id, user.Email, user.DisplayName, user.MfaEnabled, roles.ToArray(), permissions.OrderBy(x => x).ToArray());
    }

    private string CreatePurposeToken(UserAccount user, string purpose, int minutes)
    {
        var now = DateTimeOffset.UtcNow;
        var jwt = new JwtSecurityToken(_options.Issuer, _options.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(SecurityConstants.PurposeClaim, purpose), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())],
            now.UtcDateTime, now.AddMinutes(minutes).UtcDateTime, new SigningCredentials(GetKey(), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private Guid? ValidatePurposeToken(string token, string purpose)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(), out _);
            if (principal.FindFirst(SecurityConstants.PurposeClaim)?.Value != purpose) return null;
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch { return null; }
    }

    private TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuerSigningKey = true, IssuerSigningKey = GetKey(), ValidateIssuer = true, ValidIssuer = _options.Issuer,
        ValidateAudience = true, ValidAudience = _options.Audience, ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role
    };
    private async Task<ApplicationUser> RequireUser(Guid id) => await userManager.FindByIdAsync(id.ToString()) ?? throw new InvalidOperationException("Usuario no encontrado.");
    private static UserAccount Map(ApplicationUser user) => new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.TwoFactorEnabled, user.LockoutEnd);
    private SymmetricSecurityKey GetKey() => new(Encoding.UTF8.GetBytes(_options.SigningKey));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
