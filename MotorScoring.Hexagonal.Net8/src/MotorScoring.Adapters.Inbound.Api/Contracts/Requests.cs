using System.ComponentModel.DataAnnotations;

namespace MotorScoring.Adapters.Inbound.Api.Contracts;

public sealed record SolicitanteRequest(
    [Required]
    string TipoDocumento,

    [Required]
    string NumeroDocumento,

    [Required, MaxLength(150)]
    string NombresRazonSocial,

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    decimal IngresosMensuales,

    [Range(typeof(decimal), "0", "9999999999999999")]
    decimal GastosMensuales,

    [Range(typeof(decimal), "0", "9999999999999999")]
    decimal ObligacionesFinancieras,

    [Range(0, int.MaxValue)]
    int AntiguedadLaboralNegocio,

    [Range(0, int.MaxValue)]
    int NumeroObligacionesActivas,

    [Range(0, 100)]
    int PuntajeHistorialPagos,

    [Range(0, int.MaxValue)]
    int AlertasMora
);

public sealed record RegistrarSolicitudRequest(
    [Required, MaxLength(100)]
    string IdentificadorExterno,

    [Required]
    SolicitanteRequest Solicitante,

    [Required, MaxLength(30)]
    string CodigoProducto,

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    decimal MontoSolicitado,

    [Range(1, int.MaxValue)]
    int PlazoSolicitado,

    [Required]
    string Moneda,

    [Required, MaxLength(150)]
    string FinalidadCredito,

    [Required, MaxLength(50)]
    string CanalOrigen
);