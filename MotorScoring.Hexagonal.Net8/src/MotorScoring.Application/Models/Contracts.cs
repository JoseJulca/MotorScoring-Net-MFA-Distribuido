using MotorScoring.Domain.Enums;
namespace MotorScoring.Application.Models;

public sealed record RegistrarSolicitudCommand(string IdentificadorExterno, TipoDocumento TipoDocumento, string NumeroDocumento, 
    string NombresRazonSocial, decimal IngresosMensuales, decimal GastosMensuales, decimal ObligacionesFinancieras, int AntiguedadLaboralNegocio, 
    int NumeroObligacionesActivas, int PuntajeHistorialPagos, int AlertasMora, string CodigoProducto, decimal MontoSolicitado, int PlazoSolicitado, 
    Moneda Moneda, string FinalidadCredito, string CanalOrigen);
public sealed record RegistrarSolicitudResult(Guid IdSolicitud, Guid IdSolicitante, string IdentificadorExterno, string CodigoProducto, 
    decimal MontoSolicitado, int PlazoSolicitado, string Moneda, string Estado, DateTimeOffset FechaRegistro);
public sealed record EvaluarScoringCommand(Guid IdSolicitud);
public sealed record ResultadoFactorResult(string Factor, decimal ValorEvaluado, decimal PesoAplicado, int PuntajeBase, int PuntajeObtenido, 
    string ReglaAplicada, string Observacion, bool Excluyente, ResultadoScoring? ResultadoExcluyente);
public sealed record EvaluarScoringResult(Guid IdEvaluacion, Guid IdSolicitud, int PuntajeTotal, string Resultado, string Estado, string VersionModelo, 
    DateTimeOffset FechaEvaluacion, IReadOnlyList<ResultadoFactorResult> Factores);