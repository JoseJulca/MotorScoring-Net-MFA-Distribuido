using MotorScoring.Domain.Enums;
using MotorScoring.Domain.ValueObjects;
namespace MotorScoring.Domain.Entities;

public sealed record ResultadoFactor(Guid IdFactor, string CodigoFactor, decimal ValorEvaluado, decimal PesoAplicado, int PuntajeBase, int PuntajeObtenido, 
    string ReglaAplicada, string Observacion, bool ReglaExcluyente, ResultadoScoring? ResultadoExcluyente);
public sealed class EvaluacionCrediticia
{
    public Guid Id { get; }
    public Guid IdSolicitud { get; }
    public Guid IdVersionModelo { get; }
    public DateTimeOffset FechaEvaluacion { get; }
    public PuntajeCrediticio PuntajeTotal { get; }
    public ResultadoScoring Resultado { get; }
    public EstadoEvaluacion Estado { get; }
    public IReadOnlyList<ResultadoFactor> ResultadosFactor { get; }
    public EvaluacionCrediticia(Guid id, Guid solicitud, Guid version, DateTimeOffset fecha, PuntajeCrediticio puntaje, ResultadoScoring resultado, EstadoEvaluacion estado, 
        IEnumerable<ResultadoFactor> factores) 
    { 
        Id = id; 
        IdSolicitud = solicitud; 
        IdVersionModelo = version; 
        FechaEvaluacion = fecha; 
        PuntajeTotal = puntaje; 
        Resultado = resultado; 
        Estado = estado; 
        ResultadosFactor = factores.ToList().AsReadOnly(); 
    }
    public static EvaluacionCrediticia Crear(Guid solicitud, Guid version, DateTimeOffset fecha, PuntajeCrediticio puntaje, ResultadoScoring resultado, EstadoEvaluacion estado, 
        IEnumerable<ResultadoFactor> factores) => new(Guid.NewGuid(), solicitud, version, fecha, puntaje, resultado, estado, factores);
}
