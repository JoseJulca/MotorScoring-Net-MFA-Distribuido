using MotorScoring.Application.Models;
namespace MotorScoring.Application.Ports.In;

public interface IRegistrarSolicitudUseCase
{
    Task<RegistrarSolicitudResult> ExecuteAsync(RegistrarSolicitudCommand command, CancellationToken ct =
        default);
}
public interface IEvaluarScoringUseCase
{
    Task<EvaluarScoringResult> ExecuteAsync(EvaluarScoringCommand command, CancellationToken ct =
        default);
}