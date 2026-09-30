using MotorScoring.Web.Models.Requests;
using MotorScoring.Web.Models.Responses;

namespace MotorScoring.Web.Services;

public interface ISolicitudCreditoApiService
{
    Task<RegistrarSolicitudResponse> RegistrarAsync(
        RegistrarSolicitudRequest request,
        CancellationToken cancellationToken = default);

    Task<EvaluacionScoringResponse> EvaluarAsync(
        Guid idSolicitud,
        CancellationToken cancellationToken = default);
}
