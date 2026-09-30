using MotorScoring.Adapters.Outbound.Persistence.Models;
using MotorScoring.Domain.Entities;
using MotorScoring.Domain.Enums;
using MotorScoring.Domain.ValueObjects;

namespace MotorScoring.Adapters.Outbound.Persistence.Mappers;

internal static class PersistenceMapper
{
    public static Solicitante ToDomain(this SolicitanteModel model)
    {
        return Solicitante.Reconstituir(
            model.IdSolicitante,
            new NumeroDocumento(
                Enum.Parse<TipoDocumento>(model.TipoDocumento),
                model.NumeroDocumento),
            model.NombresRazonSocial,
            new Dinero(
                model.IngresosMensuales,
                Enum.Parse<Moneda>(model.Moneda)),
            new Dinero(
                model.GastosMensuales,
                Enum.Parse<Moneda>(model.Moneda)),
            new Dinero(
                model.ObligacionesFinancieras,
                Enum.Parse<Moneda>(model.Moneda)),
            model.AntiguedadLaboralNegocio,
            model.NumeroObligacionesActivas,
            model.PuntajeHistorialPagos,
            model.AlertasMora,
            Enum.Parse<EstadoRegistro>(model.Estado),
            model.FechaRegistro);
    }

    public static SolicitanteModel ToModel(this Solicitante domain)
    {
        return new SolicitanteModel
        {
            IdSolicitante = domain.Id,
            TipoDocumento = domain.Documento.Tipo.ToString(),
            NumeroDocumento = domain.Documento.Numero,
            NombresRazonSocial = domain.NombresRazonSocial,
            IngresosMensuales = domain.IngresosMensuales.Monto,
            GastosMensuales = domain.GastosMensuales.Monto,
            ObligacionesFinancieras = domain.ObligacionesFinancieras.Monto,
            AntiguedadLaboralNegocio = domain.AntiguedadLaboralNegocio,
            NumeroObligacionesActivas = domain.NumeroObligacionesActivas,
            PuntajeHistorialPagos = domain.PuntajeHistorialPagos,
            AlertasMora = domain.AlertasMora,
            Moneda = domain.Moneda.ToString(),
            Estado = domain.Estado.ToString(),
            FechaRegistro = domain.FechaRegistro
        };
    }

    public static SolicitudCredito ToDomain(this SolicitudCreditoModel model)
    {
        return SolicitudCredito.Reconstituir(
            model.IdSolicitud,
            model.IdSolicitante,
            model.IdProducto,
            new Dinero(
                model.MontoSolicitado,
                Enum.Parse<Moneda>(model.Moneda)),
            model.PlazoSolicitado,
            model.FinalidadCredito,
            model.CanalOrigen,
            model.FechaRegistro,
            new IdentificadorExterno(model.IdentificadorExterno),
            Enum.Parse<EstadoSolicitud>(model.Estado));
    }

    public static SolicitudCreditoModel ToModel(this SolicitudCredito domain)
    {
        return new SolicitudCreditoModel
        {
            IdSolicitud = domain.Id,
            IdSolicitante = domain.IdSolicitante,
            IdProducto = domain.IdProducto,
            MontoSolicitado = domain.MontoSolicitado.Monto,
            PlazoSolicitado = domain.PlazoSolicitado,
            Moneda = domain.MontoSolicitado.Moneda.ToString(),
            FinalidadCredito = domain.FinalidadCredito,
            CanalOrigen = domain.CanalOrigen,
            FechaRegistro = domain.FechaRegistro,
            IdentificadorExterno = domain.IdentificadorExterno.Valor,
            Estado = domain.Estado.ToString()
        };
    }

    public static ProductoCrediticio ToDomain(this ProductoCrediticioModel model)
    {
        return new ProductoCrediticio(
            model.IdProducto,
            model.Codigo,
            model.Nombre,
            new Dinero(
                model.MontoMinimo,
                Enum.Parse<Moneda>(model.Moneda)),
            new Dinero(
                model.MontoMaximo,
                Enum.Parse<Moneda>(model.Moneda)),
            model.PlazoMinimo,
            model.PlazoMaximo,
            Enum.Parse<EstadoProducto>(model.Estado),
            model.IdModeloScoring);
    }

    public static ModeloScoring ToDomain(this ModeloScoringModel model)
    {
        var versiones = model.Versiones
            .Select(version =>
            {
                var factores = version.Factores
                    .Select(factor =>
                    {
                        var reglas = factor.Reglas
                            .Select(regla =>
                                new ReglaEvaluacion(
                                    regla.IdRegla,
                                    regla.IdFactor,
                                    regla.Codigo,
                                    regla.Descripcion,
                                    regla.ValorMinimo,
                                    regla.ValorMaximo,
                                    regla.Puntaje,
                                    regla.EsExcluyente,
                                    string.IsNullOrWhiteSpace(
                                        regla.ResultadoExcluyente)
                                        ? null
                                        : Enum.Parse<ResultadoScoring>(
                                            regla.ResultadoExcluyente),
                                    Enum.Parse<EstadoRegla>(
                                        regla.Estado)))
                            .ToList();

                        return new FactorScoring(
                            factor.IdFactor,
                            factor.IdVersionModelo,
                            factor.Codigo,
                            factor.Nombre,
                            factor.Descripcion,
                            new Porcentaje(factor.Peso),
                            Enum.Parse<EstadoFactor>(factor.Estado),
                            reglas);
                    })
                    .ToList();

                return new VersionModelo(
                    version.IdVersionModelo,
                    version.IdModelo,
                    version.NumeroVersion,
                    version.FechaInicioVigencia,
                    version.FechaFinVigencia,
                    Enum.Parse<EstadoVersionModelo>(
                        version.Estado),
                    factores);
            })
            .ToList();

        return new ModeloScoring(
            model.IdModelo,
            model.Codigo,
            model.Nombre,
            model.Descripcion,
            Enum.Parse<EstadoModelo>(model.Estado),
            model.FechaCreacion,
            versiones);
    }
}