using System.Net.Http.Headers;
using System.Net.Http.Json;
using MotorScoring.Web.Models.Auth;

namespace MotorScoring.Web.Services;

public sealed class IdentityApiService(HttpClient httpClient) : IIdentityApiService
{
    public Task<IdentityLoginResponse> LoginAsync(string email, string password, CancellationToken ct = default) =>
        PostAsync<IdentityLoginResponse>("/api/auth/login", new { email, password }, null, ct);

    public Task<IdentityLoginResponse> VerifyRecoveryCodeAsync(string challengeToken, string recoveryCode, CancellationToken ct = default) =>
        PostAsync<IdentityLoginResponse>("/api/auth/mfa/recovery", new { challengeToken, recoveryCode }, null, ct);

    public Task<IdentityLoginResponse> VerifyMfaAsync(string challengeToken, string code, CancellationToken ct = default) =>
        PostAsync<IdentityLoginResponse>("/api/auth/mfa/verify", new { challengeToken, code }, null, ct);

    public Task<IdentityLoginResponse> ExchangeExternalAsync(string code, CancellationToken ct = default) =>
        PostAsync<IdentityLoginResponse>("/api/auth/external/exchange", new { code }, null, ct);

    public Task<IdentityTokens> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
        PostAsync<IdentityTokens>("/api/auth/refresh", new { refreshToken }, null, ct);

    public Task<IdentityMfaSetupResponse> SetupMfaAsync(string accessToken, CancellationToken ct = default) =>
        PostAsync<IdentityMfaSetupResponse>("/api/auth/mfa/setup", new { }, accessToken, ct);

    public Task<IdentityMfaEnableResponse> EnableMfaAsync(string accessToken, string code, CancellationToken ct = default) =>
        PostAsync<IdentityMfaEnableResponse>("/api/auth/mfa/enable", new { code }, accessToken, ct);

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/auth/revoke", new { refreshToken }, ct);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            throw new ApiException((int)response.StatusCode, "No se pudo revocar la sesión en Identity.");
    }

    private async Task<T> PostAsync<T>(string url, object body, string? bearer, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (!string.IsNullOrWhiteSpace(bearer)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            throw new ApiException((int)response.StatusCode, string.IsNullOrWhiteSpace(raw) ? "Error de autenticación." : raw);
        }
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
               ?? throw new ApiException(500, "Identity no devolvió una respuesta válida.");
    }
}
