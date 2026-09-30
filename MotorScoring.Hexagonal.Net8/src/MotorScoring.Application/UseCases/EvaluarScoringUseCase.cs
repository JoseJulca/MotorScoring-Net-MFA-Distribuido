using MotorScoring.Application.Exceptions;
using MotorScoring.Application.Models;
using MotorScoring.Application.Ports.In;
using MotorScoring.Application.Ports.Out;
using MotorScoring.Domain.Exceptions;
using MotorScoring.Domain.Services;
namespace MotorScoring.Application.UseCases;

public sealed class EvaluarScoringUseCase(ISolicitudCreditoRepository solicitudes, ISolicitanteRepository solicitantes, IProductoCrediticioRepository productos, IModeloScoringRepository modelos, IEvaluacionCrediticiaRepository evaluaciones, IUnitOfWork uow, CalculadorScoring calculador, TimeProvider time) : IEvaluarScoringUseCase
{
    public async Task<EvaluarScoringResult> ExecuteAsync(EvaluarScoringCommand cmd, CancellationToken ct =
        default)
    {
        var solicitud = await solicitudes.BuscarPorIdAsync(cmd.IdSolicitud, ct) ??
            throw new RecursoNoEncontradoException("Solicitud no encontrada: " + cmd.IdSolicitud);

        if (!solicitud.EstaRegistrada()) throw new SolicitudNoEvaluableException("La solicitud ya fue evaluada o no está registrada.");

        var solicitante = await solicitantes.BuscarPorIdAsync(solicitud.IdSolicitante, ct) ??
            throw new RecursoNoEncontradoException("Solicitante no encontrado.");

        if (!solicitante.TieneDatosFinancierosCompletos() || !solicitante.TieneIngresosValidos()) throw new SolicitudNoEvaluableException("Información financiera incompleta o inválida.");

        var producto = await productos.BuscarPorIdAsync(solicitud.IdProducto, ct) ??
            throw new RecursoNoEncontradoException("Producto no encontrado.");

        producto.ValidarSolicitud(solicitud.MontoSolicitado, solicitud.PlazoSolicitado);
        
        var ahora = time.GetUtcNow();
        var modelo = await modelos.BuscarCompletoPorIdAsync(producto.IdModeloScoring, ct) ??
            throw new RecursoNoEncontradoException("Modelo no encontrado.");
        
        var version = modelo.VersionActiva(DateOnly.FromDateTime(ahora.UtcDateTime));
        version.ValidarPesos();
        
        if (await evaluaciones.ExistePorSolicitudYVersionAsync(solicitud.Id, version.Id, ct)) throw new SolicitudDuplicadaException("La solicitud ya fue evaluada con la versión activa.");
        
        var evaluacion = calculador.Calcular(solicitud, solicitante, version, ahora);
        
        await evaluaciones.GuardarAsync(evaluacion, ct);
        
        solicitud.MarcarEvaluada();
        
        await solicitudes.GuardarAsync(solicitud, ct);
        
        await uow.CommitAsync(ct);
        
        var factores = evaluacion.ResultadosFactor.Select(r => new ResultadoFactorResult(r.CodigoFactor, r.ValorEvaluado, r.PesoAplicado, r.PuntajeBase, r.PuntajeObtenido, r.ReglaAplicada, r.Observacion, r.ReglaExcluyente, r.ResultadoExcluyente)).ToList();
        
        return new(evaluacion.Id, evaluacion.IdSolicitud, evaluacion.PuntajeTotal.Valor, evaluacion.Resultado.ToString(), evaluacion.Estado.ToString(), version.NumeroVersion, evaluacion.FechaEvaluacion, factores);
    }
}