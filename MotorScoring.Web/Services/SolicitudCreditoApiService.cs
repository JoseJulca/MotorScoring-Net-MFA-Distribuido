using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using MotorScoring.Web.Models.Requests;
using MotorScoring.Web.Models.Responses;

namespace MotorScoring.Web.Services;

public sealed class SolicitudCreditoApiService(HttpClient httpClient, TokenSessionService tokenSession) : ISolicitudCreditoApiService
{
    public async Task<RegistrarSolicitudResponse> RegistrarAsync(RegistrarSolicitudRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/solicitudes-credito") { Content = JsonContent.Create(request) };
        await AddBearerAsync(message);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await BuildExceptionAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<RegistrarSolicitudResponse>(cancellationToken: cancellationToken)
               ?? throw new ApiException(500, "La API del Motor de Scoring no devolvió una respuesta válida al registrar la solicitud.");
    }

    public async Task<EvaluacionScoringResponse> EvaluarAsync(Guid idSolicitud, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/solicitudes-credito/{idSolicitud}/evaluar");
        await AddBearerAsync(message);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await BuildExceptionAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<EvaluacionScoringResponse>(cancellationToken: cancellationToken)
               ?? throw new ApiException(500, "La API del Motor de Scoring no devolvió una respuesta válida al evaluar la solicitud.");
    }

    private async Task AddBearerAsync(HttpRequestMessage message)
    {
        var token = await tokenSession.GetValidAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token)) throw new ApiException(401, "La sesión no contiene un access token válido.");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<ApiException> BuildExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return new ApiException(401, "El API rechazó la credencial. Inicie sesión nuevamente.");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return new ApiException(403, "El usuario no tiene permiso para realizar esta operación.");
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken: cancellationToken);
            if (error is not null && !string.IsNullOrWhiteSpace(error.Message)) return new ApiException((int)response.StatusCode, error.Message);
        }
        catch { }
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ApiException((int)response.StatusCode,
            !string.IsNullOrWhiteSpace(raw) ? raw : $"La API respondió con HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
    }
}
