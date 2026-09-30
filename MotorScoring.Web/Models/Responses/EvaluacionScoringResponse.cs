namespace MotorScoring.Web.Models.Responses;

public sealed record EvaluacionScoringResponse(
    Guid IdEvaluacion,
    Guid IdSolicitud,
    int PuntajeTotal,
    string Resultado,
    string Estado,
    string VersionModelo,
    DateTimeOffset FechaEvaluacion,
    IReadOnlyList<ResultadoFactorResponse> Factores);
