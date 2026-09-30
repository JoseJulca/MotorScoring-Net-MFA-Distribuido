namespace MotorScoring.Web.Models.Responses;

public sealed record RegistrarSolicitudResponse(
    Guid IdSolicitud,
    Guid IdSolicitante,
    string IdentificadorExterno,
    string CodigoProducto,
    decimal MontoSolicitado,
    int PlazoSolicitado,
    string Moneda,
    string Estado,
    DateTimeOffset FechaRegistro);
