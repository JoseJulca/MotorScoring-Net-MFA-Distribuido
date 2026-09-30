namespace MotorScoring.Notification.Worker.Models;
public sealed record EvaluacionCompletada(Guid EventId, Guid IdSolicitud, Guid IdEvaluacion, string Resultado, int Puntaje, string VersionModelo, DateTimeOffset FechaEvaluacion, string? Email);
