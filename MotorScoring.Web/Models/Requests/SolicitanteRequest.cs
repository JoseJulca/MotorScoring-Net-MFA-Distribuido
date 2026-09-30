using System.ComponentModel.DataAnnotations;

namespace MotorScoring.Web.Models.Requests;

public sealed class SolicitanteRequest
{
    [Required]
    [Display(Name = "Tipo de documento")]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Número de documento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    [Display(Name = "Nombre / Razón Social")]
    public string NombresRazonSocial { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    [Display(Name = "Ingresos mensuales")]
    public decimal IngresosMensuales { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    [Display(Name = "Gastos mensuales")]
    public decimal GastosMensuales { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    [Display(Name = "Obligaciones financieras")]
    public decimal ObligacionesFinancieras { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Antigüedad laboral / negocio (meses)")]
    public int AntiguedadLaboralNegocio { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Número de obligaciones activas")]
    public int NumeroObligacionesActivas { get; set; }

    [Range(0, 100)]
    [Display(Name = "Puntaje historial de pagos (0 - 100)")]
    public int PuntajeHistorialPagos { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Alertas de mora")]
    public int AlertasMora { get; set; }
}
