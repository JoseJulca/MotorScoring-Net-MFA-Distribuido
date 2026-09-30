using MotorScoring.Notification.Worker.Models;
namespace MotorScoring.Notification.Worker;
public interface INotificacionProcessor { Task ProcesarAsync(EvaluacionCompletada evento, CancellationToken ct); }
public sealed class EmailSimuladoProcessor(ILogger<EmailSimuladoProcessor> logger) : INotificacionProcessor
{
    public Task ProcesarAsync(EvaluacionCompletada e, CancellationToken ct)
    { logger.LogInformation("[RABBITMQ][CORREO SIMULADO] Para: {Email}; Solicitud: {Solicitud}; Resultado: {Resultado}; Puntaje: {Puntaje}; Modelo: {Modelo}; Evento: {Evento}", e.Email ?? "demo@local.test", e.IdSolicitud, e.Resultado, e.Puntaje, e.VersionModelo, e.EventId); return Task.CompletedTask; }
}
