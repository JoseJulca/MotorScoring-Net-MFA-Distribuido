using MotorScoring.Domain.Enums;
using MotorScoring.Domain.Exceptions;
using MotorScoring.Domain.ValueObjects;
namespace MotorScoring.Domain.Entities;

public sealed class SolicitudCredito
{
    public Guid Id { get; private set; }
    public Guid IdSolicitante { get; }
    public Guid IdProducto { get; }
    public Dinero MontoSolicitado { get; }
    public int PlazoSolicitado { get; }
    public string FinalidadCredito { get; }
    public string CanalOrigen { get; }
    public DateTimeOffset FechaRegistro { get; }
    public IdentificadorExterno IdentificadorExterno { get; }
    public EstadoSolicitud Estado { get; private set; }
    private SolicitudCredito(Guid id, Guid sol, Guid prod, Dinero monto, int plazo, string fin, string canal, DateTimeOffset fecha, IdentificadorExterno ext, EstadoSolicitud estado) 
    { 
        if (monto.Monto <= 0) 
            throw new DomainException("El monto debe ser mayor que cero."); 
        if (plazo <= 0) 
            throw new DomainException("El plazo debe ser mayor que cero."); 
        if (string.IsNullOrWhiteSpace(fin)) 
            throw new DomainException("La finalidad es obligatoria."); 
        if (string.IsNullOrWhiteSpace(canal)) 
            throw new DomainException("El canal es obligatorio."); 
        Id = id; 
        IdSolicitante = sol; 
        IdProducto = prod; 
        MontoSolicitado = monto; 
        PlazoSolicitado = plazo; 
        FinalidadCredito = fin.Trim(); 
        CanalOrigen = canal.Trim(); 
        FechaRegistro = fecha; 
        IdentificadorExterno = ext; 
        Estado = estado; 
    }
    public static SolicitudCredito Registrar(Guid s, Guid p, Dinero m, int pl, string f, string c, IdentificadorExterno e, DateTimeOffset fecha) => new(Guid.NewGuid(), s, p, m, pl, f, c, fecha, e, EstadoSolicitud.REGISTRADA);
    public static SolicitudCredito Reconstituir(Guid id, Guid s, Guid p, Dinero m, int pl, string f, string c, DateTimeOffset fecha, IdentificadorExterno e, EstadoSolicitud estado) => new(id, s, p, m, pl, f, c, fecha, e, estado);
    public void MarcarEvaluada() { if (Estado != EstadoSolicitud.REGISTRADA) throw new DomainException("Solo una solicitud registrada puede evaluarse."); Estado = EstadoSolicitud.EVALUADA; }
    public bool EstaRegistrada() => Estado == EstadoSolicitud.REGISTRADA; public bool EstaEvaluable() => EstaRegistrada();
}
