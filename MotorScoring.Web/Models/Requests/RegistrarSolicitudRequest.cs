using System.ComponentModel.DataAnnotations;

namespace MotorScoring.Web.Models.Requests;

public sealed class RegistrarSolicitudRequest
{
    [Required, MaxLength(100)]
    [Display(Name = "Identificador externo")]
    public string IdentificadorExterno { get; set; } = string.Empty;

    [Required]
    public SolicitanteRequest Solicitante { get; set; } = new();

    [Required, MaxLength(30)]
    [Display(Name = "Código de producto")]
    public string CodigoProducto { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    [Display(Name = "Monto solicitado")]
    public decimal MontoSolicitado { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Plazo solicitado (meses)")]
    public int PlazoSolicitado { get; set; }

    [Required]
    [Display(Name = "Moneda")]
    public string Moneda { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    [Display(Name = "Finalidad del crédito")]
    public string FinalidadCredito { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [Display(Name = "Canal de origen")]
    public string CanalOrigen { get; set; } = string.Empty;
}
