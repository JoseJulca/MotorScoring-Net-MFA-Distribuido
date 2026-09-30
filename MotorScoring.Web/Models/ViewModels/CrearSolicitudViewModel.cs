using MotorScoring.Web.Models.Requests;
using MotorScoring.Web.Models.Responses;

namespace MotorScoring.Web.Models.ViewModels;

public sealed class CrearSolicitudViewModel
{
    public RegistrarSolicitudRequest Solicitud { get; set; } = new();

    public RegistrarSolicitudResponse? Registro { get; set; }

    public bool EstaRegistrada => Registro is not null;
}
