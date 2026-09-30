namespace MotorScoring.Adapters.Inbound.Api.Contracts;

public sealed record RegistrarSolicitudResponse(
    Guid IdSolicitud,
    Guid IdSolicitante,
    string IdentificadorExterno,
    string CodigoProducto,
    decimal MontoSolicitado,
    int PlazoSolicitado,
    string Moneda,
    string Estado,
    DateTimeOffset FechaRegistro
);

public sealed record ResultadoFactorResponse(
    string Factor,
    decimal ValorEvaluado,
    decimal PesoAplicado,
    int PuntajeBase,
    int PuntajeObtenido,
    string ReglaAplicada,
    string Observacion,
    bool Excluyente,
    string? ResultadoExcluyente
);

public sealed record EvaluacionScoringResponse(
    Guid IdEvaluacion,
    Guid IdSolicitud,
    int PuntajeTotal,
    string Resultado,
    string Estado,
    string VersionModelo,
    DateTimeOffset FechaEvaluacion,
    IReadOnlyList<ResultadoFactorResponse> Factores
);

public sealed record ErrorResponse(
    DateTimeOffset Timestamp,
    int Status,
    string Code,
    string Message,
    string Path,
    object? Validation = null
);