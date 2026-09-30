namespace MotorScoring.Web.Models.Responses;

public sealed record ResultadoFactorResponse(
    string Factor,
    decimal ValorEvaluado,
    decimal PesoAplicado,
    int PuntajeBase,
    int PuntajeObtenido,
    string ReglaAplicada,
    string Observacion,
    bool Excluyente,
    string? ResultadoExcluyente);
