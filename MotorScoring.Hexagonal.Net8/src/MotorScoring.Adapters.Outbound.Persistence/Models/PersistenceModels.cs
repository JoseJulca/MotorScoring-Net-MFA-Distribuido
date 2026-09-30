namespace MotorScoring.Adapters.Outbound.Persistence.Models;

public sealed class SolicitanteModel 
{ 
    public Guid IdSolicitante { get; set; } 
    public string TipoDocumento { get; set; } = ""; 
    public string NumeroDocumento { get; set; } = ""; 
    public string NombresRazonSocial { get; set; } = ""; 
    public decimal IngresosMensuales { get; set; }
    public decimal GastosMensuales { get; set; } 
    public decimal ObligacionesFinancieras { get; set; }
    public int AntiguedadLaboralNegocio { get; set; } 
    public int NumeroObligacionesActivas { get; set; } 
    public int PuntajeHistorialPagos { get; set; } 
    public int AlertasMora { get; set; } 
    public string Moneda { get; set; } = ""; 
    public string Estado { get; set; } = ""; 
    public DateTimeOffset FechaRegistro { get; set; } 
}

public sealed class SolicitudCreditoModel 
{ 
    public Guid IdSolicitud { get; set; } 
    public Guid IdSolicitante { get; set; } 
    public Guid IdProducto { get; set; } 
    public decimal MontoSolicitado { get; set; } 
    public int PlazoSolicitado { get; set; } 
    public string Moneda { get; set; } = ""; 
    public string FinalidadCredito { get; set; } = ""; 
    public string CanalOrigen { get; set; } = ""; 
    public DateTimeOffset FechaRegistro { get; set; } 
    public string IdentificadorExterno { get; set; } = "";
    public string Estado { get; set; } = "";
}
public sealed class ProductoCrediticioModel 
{ 
    public Guid IdProducto { get; set; } 
    public string Codigo { get; set; } = ""; 
    public string Nombre { get; set; } = ""; 
    public decimal MontoMinimo { get; set; } 
    public decimal MontoMaximo { get; set; } 
    public int PlazoMinimo { get; set; } 
    public int PlazoMaximo { get; set; } 
    public string Moneda { get; set; } = ""; 
    public string Estado { get; set; } = ""; 
    public Guid IdModeloScoring { get; set; } 
}
public sealed class ModeloScoringModel 
{ 
    public Guid IdModelo { get; set; } 
    public string Codigo { get; set; } = ""; 
    public string Nombre { get; set; } = ""; 
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = ""; 
    public DateTimeOffset FechaCreacion { get; set; } 
    public List<VersionModeloModel> Versiones { get; set; } = []; 
}
public sealed class VersionModeloModel 
{ 
    public Guid IdVersionModelo { get; set; } 
    public Guid IdModelo { get; set; } 
    public string NumeroVersion { get; set; } = ""; 
    public DateOnly FechaInicioVigencia { get; set; } 
    public DateOnly? FechaFinVigencia { get; set; } 
    public string Estado { get; set; } = ""; 
    public DateTimeOffset FechaCreacion { get; set; } 
    public List<FactorScoringModel> Factores { get; set; } = []; 
}
public sealed class FactorScoringModel 
{ 
    public Guid IdFactor { get; set; } 
    public Guid IdVersionModelo { get; set; } 
    public string Codigo { get; set; } = ""; 
    public string Nombre { get; set; } = ""; 
    public string? Descripcion { get; set; } 
    public decimal Peso { get; set; } 
    public string Estado { get; set; } = ""; 
    public List<ReglaEvaluacionModel> Reglas { get; set; } = []; 
}
public sealed class ReglaEvaluacionModel 
{ 
    public Guid IdRegla { get; set; } 
    public Guid IdFactor { get; set; } 
    public string Codigo { get; set; } = ""; 
    public string Descripcion { get; set; } = ""; 
    public decimal ValorMinimo { get; set; } 
    public decimal ValorMaximo { get; set; } 
    public int Puntaje { get; set; } 
    public bool EsExcluyente { get; set; } 
    public string? ResultadoExcluyente { get; set; } 
    public string Estado { get; set; } = ""; 
}
public sealed class EvaluacionCrediticiaModel 
{ 
    public Guid IdEvaluacion { get; set; } 
    public Guid IdSolicitud { get; set; }
    public Guid IdVersionModelo { get; set; }
    public DateTimeOffset FechaEvaluacion { get; set; } 
    public int PuntajeTotal { get; set; } 
    public string Resultado { get; set; } = "";
    public string Estado { get; set; } = "";
    public List<ResultadoFactorModel> ResultadosFactor { get; set; } = []; 
    public ResultadoScoringModel? ResultadoScoring { get; set; } 
}
public sealed class ResultadoFactorModel 
{ 
    public Guid IdResultadoFactor { get; set; } 
    public Guid IdEvaluacion { get; set; } 
    public Guid IdFactor { get; set; } 
    public string CodigoFactor { get; set; } = ""; 
    public decimal ValorEvaluado { get; set; } 
    public decimal PesoAplicado { get; set; } 
    public int PuntajeBase { get; set; } 
    public int PuntajeObtenido { get; set; } 
    public string ReglaAplicada { get; set; } = ""; 
    public string? Observacion { get; set; } 
    public bool ReglaExcluyente { get; set; } 
    public string? ResultadoExcluyente { get; set; } 
}
public sealed class ResultadoScoringModel 
{ 
    public Guid IdResultadoScoring { get; set; } 
    public Guid IdEvaluacion { get; set; } 
    public int PuntajeTotal { get; set; } 
    public string Resultado { get; set; } = ""; 
    public DateTimeOffset FechaResultado { get; set; } 
}
