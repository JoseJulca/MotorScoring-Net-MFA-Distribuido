using MotorScoring.Domain.Entities;
using MotorScoring.Domain.ValueObjects;
namespace MotorScoring.Application.Ports.Out;

public interface ISolicitanteRepository
{
    Task<Solicitante?> BuscarPorDocumentoAsync(NumeroDocumento documento, CancellationToken ct);
    Task<Solicitante?> BuscarPorIdAsync(Guid id, CancellationToken ct);
    Task GuardarAsync(Solicitante solicitante, CancellationToken ct);
}
public interface ISolicitudCreditoRepository
{
    Task<bool> ExistePorIdentificadorExternoAsync(IdentificadorExterno id, CancellationToken ct);
    Task<SolicitudCredito?> BuscarPorIdAsync(Guid id, CancellationToken ct);
    Task GuardarAsync(SolicitudCredito solicitud, CancellationToken ct);
}
public interface IProductoCrediticioRepository
{
    Task<ProductoCrediticio?> BuscarPorCodigoAsync(string codigo, CancellationToken ct);
    Task<ProductoCrediticio?> BuscarPorIdAsync(Guid id, CancellationToken ct);
}
public interface IModeloScoringRepository
{
    Task<ModeloScoring?> BuscarCompletoPorIdAsync(Guid id, CancellationToken ct);
}
public interface IEvaluacionCrediticiaRepository
{
    Task<bool> ExistePorSolicitudYVersionAsync(Guid solicitud, Guid version, CancellationToken ct);
    Task GuardarAsync(EvaluacionCrediticia evaluacion, CancellationToken ct);
}
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct);
}
public interface IEvaluacionNotificacionPort { Task InformarAsync(MotorScoring.Application.Models.EvaluarScoringResult resultado, CancellationToken ct); }
