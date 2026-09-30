using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MotorScoring.Adapters.Inbound.Api.Contracts;
using MotorScoring.Application.Models;
using MotorScoring.Application.Ports.In;
using MotorScoring.Domain.Enums;

namespace MotorScoring.Adapters.Inbound.Api.Controllers;

[ApiController]
[Route("api/v1/solicitudes-credito")]
public sealed class SolicitudesCreditoController(IRegistrarSolicitudUseCase crear, IEvaluarScoringUseCase evaluar, MotorScoring.Application.Ports.Out.IEvaluacionNotificacionPort notificaciones) : ControllerBase
{
    [Authorize(Policy = "Scoring.Solicitud.Crear")]
    [HttpPost]
    [ProducesResponseType(typeof(RegistrarSolicitudResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegistrarSolicitudResponse>> Registrar([FromBody] RegistrarSolicitudRequest r, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoDocumento>(r.Solicitante.TipoDocumento, true, out
                var tipo)) throw new ArgumentException("Tipo de documento inválido.");
        if (!Enum.TryParse<Moneda>(r.Moneda, true, out
                var moneda)) throw new ArgumentException("Moneda inválida.");
        var s = r.Solicitante;
        var d = await crear.ExecuteAsync(new(r.IdentificadorExterno, tipo, s.NumeroDocumento, s.NombresRazonSocial, s.IngresosMensuales, s.GastosMensuales, 
            s.ObligacionesFinancieras, s.AntiguedadLaboralNegocio, s.NumeroObligacionesActivas, s.PuntajeHistorialPagos, s.AlertasMora, r.CodigoProducto, 
            r.MontoSolicitado, r.PlazoSolicitado, moneda, r.FinalidadCredito, r.CanalOrigen), ct);
        var response = new RegistrarSolicitudResponse(d.IdSolicitud, d.IdSolicitante, d.IdentificadorExterno, d.CodigoProducto, d.MontoSolicitado, d.PlazoSolicitado, d.Moneda, d.Estado, d.FechaRegistro);
        return Created($"/api/v1/solicitudes-credito/{d.IdSolicitud}", response);
    }

    [Authorize(Policy = "Scoring.Evaluacion.Ejecutar")]
    [HttpPost("{id:guid}/evaluar")]
    [ProducesResponseType(typeof(EvaluacionScoringResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EvaluacionScoringResponse>> Evaluar(Guid id, CancellationToken ct)
    {
        var d = await evaluar.ExecuteAsync(new(id), ct);
        await notificaciones.InformarAsync(d, ct);
        return Ok(new EvaluacionScoringResponse(d.IdEvaluacion, d.IdSolicitud, d.PuntajeTotal, d.Resultado, d.Estado, d.VersionModelo, d.FechaEvaluacion, 
            d.Factores.Select(x => new ResultadoFactorResponse(x.Factor, x.ValorEvaluado, x.PesoAplicado, x.PuntajeBase, x.PuntajeObtenido, x.ReglaAplicada, 
            x.Observacion, x.Excluyente, x.ResultadoExcluyente?.ToString())).ToList()));
    }
}