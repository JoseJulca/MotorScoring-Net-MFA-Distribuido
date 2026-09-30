using MotorScoring.Notificaciones.Application.Ports;
using MotorScoring.Notificaciones.Application.Contracts;
namespace MotorScoring.Notificaciones.Application.UseCases;
public sealed class PublicarEvaluacion(INotificacionPublisher notificaciones, IAuditoriaPublisher auditoria)
{
    public async Task ExecuteAsync(EvaluacionCompletada evento, CancellationToken ct)
    {
        if (evento.EventId == Guid.Empty || evento.IdSolicitud == Guid.Empty || evento.IdEvaluacion == Guid.Empty) throw new ArgumentException("Identificadores requeridos.");
        await notificaciones.PublicarAsync(evento, ct);
        await auditoria.PublicarAsync(evento, ct);
    }
}
