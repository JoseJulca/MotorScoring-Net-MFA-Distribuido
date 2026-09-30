using System.Net.Http.Json;
using MotorScoring.Application.Models;
namespace MotorScoring.Api.Integration;
public sealed class NotificacionesClient(HttpClient http, IConfiguration config, ILogger<NotificacionesClient> logger) : MotorScoring.Application.Ports.Out.IEvaluacionNotificacionPort
{
    public async Task InformarAsync(EvaluarScoringResult result, CancellationToken ct)
    {
        var evento = new { EventId = Guid.NewGuid(), result.IdSolicitud, result.IdEvaluacion, result.Resultado, Puntaje = result.PuntajeTotal, result.VersionModelo, result.FechaEvaluacion, Email = (string?)null };
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/notificaciones/evaluacion") { Content = JsonContent.Create(evento) };
        request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", config["Notificaciones:ApiKey"]);
        try { using var response = await http.SendAsync(request, ct); if (!response.IsSuccessStatusCode) logger.LogWarning("Notificaciones respondió {StatusCode} para evaluación {Id}", response.StatusCode, result.IdEvaluacion); }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested) { logger.LogWarning(ex, "No se pudo informar evaluación {Id} a Notificaciones", result.IdEvaluacion); }
    }
}
