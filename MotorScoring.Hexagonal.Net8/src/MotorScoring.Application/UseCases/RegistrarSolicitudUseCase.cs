using MotorScoring.Application.Exceptions;
using MotorScoring.Application.Models;
using MotorScoring.Application.Ports.In;
using MotorScoring.Application.Ports.Out;
using MotorScoring.Domain.Entities;
using MotorScoring.Domain.ValueObjects;
namespace MotorScoring.Application.UseCases;

public sealed class RegistrarSolicitudUseCase(ISolicitanteRepository solicitantes, ISolicitudCreditoRepository solicitudes, IProductoCrediticioRepository productos, IUnitOfWork uow, TimeProvider time) : IRegistrarSolicitudUseCase
{
    public async Task<RegistrarSolicitudResult> ExecuteAsync(RegistrarSolicitudCommand c, CancellationToken ct =
        default)
    {
        var ext = new IdentificadorExterno(c.IdentificadorExterno);
        if (await solicitudes.ExistePorIdentificadorExternoAsync(ext, ct)) throw new SolicitudDuplicadaException("Ya existe una solicitud con identificador externo " + ext.Valor);
        var producto = await productos.BuscarPorCodigoAsync(c.CodigoProducto, ct) ??
            throw new RecursoNoEncontradoException("Producto no encontrado: " + c.CodigoProducto);
        var doc = new NumeroDocumento(c.TipoDocumento, c.NumeroDocumento);
        var ingresos = new Dinero(c.IngresosMensuales, c.Moneda);
        var gastos = new Dinero(c.GastosMensuales, c.Moneda);
        var obligaciones = new Dinero(c.ObligacionesFinancieras, c.Moneda);
        var ahora = time.GetUtcNow();
        var solicitante = await solicitantes.BuscarPorDocumentoAsync(doc, ct);
        if (solicitante is null) solicitante = Solicitante.Registrar(doc, c.NombresRazonSocial, ingresos, gastos, obligaciones, c.AntiguedadLaboralNegocio, c.NumeroObligacionesActivas, c.PuntajeHistorialPagos, c.AlertasMora, ahora);
        else solicitante.ActualizarPerfil(c.NombresRazonSocial, ingresos, gastos, obligaciones, c.AntiguedadLaboralNegocio, c.NumeroObligacionesActivas, c.PuntajeHistorialPagos, c.AlertasMora);
        await solicitantes.GuardarAsync(solicitante, ct);
        var solicitud = SolicitudCredito.Registrar(solicitante.Id, producto.Id, Dinero.Positivo(c.MontoSolicitado, c.Moneda), c.PlazoSolicitado, c.FinalidadCredito, c.CanalOrigen, ext, ahora);
        await solicitudes.GuardarAsync(solicitud, ct);
        await uow.CommitAsync(ct);
        return new(solicitud.Id, solicitante.Id, ext.Valor, producto.Codigo, solicitud.MontoSolicitado.Monto, solicitud.PlazoSolicitado, solicitud.MontoSolicitado.Moneda.ToString(), solicitud.Estado.ToString(), solicitud.FechaRegistro);
    }
}