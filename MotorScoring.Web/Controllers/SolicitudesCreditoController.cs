using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorScoring.Web.Models.Requests;
using MotorScoring.Web.Models.Responses;
using MotorScoring.Web.Models.ViewModels;
using MotorScoring.Web.Services;

namespace MotorScoring.Web.Controllers;

[Authorize]
public sealed class SolicitudesCreditoController : Controller
{
    private readonly ISolicitudCreditoApiService _apiService;

    public SolicitudesCreditoController(ISolicitudCreditoApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View(new CrearSolicitudViewModel
        {
            Solicitud = BuildDefaultRequest()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        CrearSolicitudViewModel model,
        CancellationToken cancellationToken)
    {
        // Registro es información de salida. No participa en la validación del input.
        ModelState.Remove(nameof(CrearSolicitudViewModel.Registro));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var registro = await _apiService.RegistrarAsync(
                model.Solicitud,
                cancellationToken);

            // Paso 1 termina aquí. NO se llama al API de evaluación.
            model.Registro = registro;

            ViewData["RegistroExitoso"] =
                $"Solicitud {registro.IdentificadorExterno} registrada correctamente.";

            return View(model);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                string.Empty,
                "No fue posible comunicarse con la API del Motor de Scoring.");

            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluar(
        CrearSolicitudViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Registro is null || model.Registro.IdSolicitud == Guid.Empty)
        {
            ModelState.AddModelError(
                string.Empty,
                "Primero debe registrar la solicitud antes de evaluarla.");

            return View("Crear", model);
        }

        try
        {
            // Paso 2: se usa exactamente el IdSolicitud devuelto por el registro.
            var resultado = await _apiService.EvaluarAsync(
                model.Registro.IdSolicitud,
                cancellationToken);

            return View("Resultado", resultado);
        }
        catch (ApiException ex)
        {
            // Si la evaluación falla, conservamos la misma solicitud registrada
            // y el mismo IdentificadorExterno para poder visualizar el error
            // sin intentar registrarla nuevamente.
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Crear", model);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                string.Empty,
                "No fue posible comunicarse con la API del Motor de Scoring.");

            return View("Crear", model);
        }
    }

    private static RegistrarSolicitudRequest BuildDefaultRequest() => new()
    {
        IdentificadorExterno = "WEB-2026-000001",
        Solicitante = new SolicitanteRequest
        {
            TipoDocumento = "DNI",
            NumeroDocumento = "12345678",
            NombresRazonSocial = "Juan Perez",
            IngresosMensuales = 5000m,
            GastosMensuales = 1500m,
            ObligacionesFinancieras = 500m,
            AntiguedadLaboralNegocio = 36,
            NumeroObligacionesActivas = 1,
            PuntajeHistorialPagos = 85,
            AlertasMora = 0
        },
        CodigoProducto = "PRESTAMO_PERSONAL",
        MontoSolicitado = 15000m,
        PlazoSolicitado = 24,
        Moneda = "PEN",
        FinalidadCredito = "Capital de trabajo",
        CanalOrigen = "WEB"
    };
}
