using MotorScoring.Notificaciones.Application.Contracts;
namespace MotorScoring.Notificaciones.Application.Ports;
public interface INotificacionPublisher { Task PublicarAsync(EvaluacionCompletada evento, CancellationToken ct); }
public interface IAuditoriaPublisher { Task PublicarAsync(EvaluacionCompletada evento, CancellationToken ct); }
