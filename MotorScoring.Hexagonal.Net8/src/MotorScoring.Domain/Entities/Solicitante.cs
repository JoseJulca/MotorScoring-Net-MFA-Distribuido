using MotorScoring.Domain.Enums;
using MotorScoring.Domain.Exceptions;
using MotorScoring.Domain.ValueObjects;
namespace MotorScoring.Domain.Entities;

public sealed class Solicitante
{
    public Guid Id { get; private set; }
    public NumeroDocumento Documento { get; private set; }
    public string NombresRazonSocial { get; private set; }
    public Dinero IngresosMensuales { get; private set; }
    public Dinero GastosMensuales { get; private set; }
    public Dinero ObligacionesFinancieras { get; private set; }
    public int AntiguedadLaboralNegocio { get; private set; }
    public int NumeroObligacionesActivas { get; private set; }
    public int PuntajeHistorialPagos { get; private set; }
    public int AlertasMora { get; private set; }
    public EstadoRegistro Estado { get; private set; }
    public DateTimeOffset FechaRegistro { get; private set; }
    public Moneda Moneda => IngresosMensuales.Moneda;
    private Solicitante(Guid id, NumeroDocumento documento, string nombres, Dinero ingresos, Dinero gastos, Dinero obligaciones, int antiguedad, int activas, int historial, 
        int alertas, EstadoRegistro estado, DateTimeOffset fecha)
    {
        Id = id;
        Documento = documento;
        NombresRazonSocial = Validar(nombres, ingresos, gastos, obligaciones, antiguedad, activas, historial, alertas);
        IngresosMensuales = ingresos;
        GastosMensuales = gastos;
        ObligacionesFinancieras = obligaciones;
        AntiguedadLaboralNegocio = antiguedad;
        NumeroObligacionesActivas = activas;
        PuntajeHistorialPagos = historial;
        AlertasMora = alertas;
        Estado = estado;
        FechaRegistro = fecha;
    }
    private static string Validar(string nombres, Dinero i, Dinero g, Dinero o, int a, int ac, int h, int am)
    {
        if (string.IsNullOrWhiteSpace(nombres)) throw new DomainException("Los nombres o razón social son obligatorios.");
        if (i.Monto <= 0) throw new DomainException("Los ingresos deben ser mayores que cero.");
        if (i.Moneda != g.Moneda || i.Moneda != o.Moneda) throw new DomainException("Los datos financieros deben usar la misma moneda.");
        if (a < 0 || ac < 0 || am < 0) throw new DomainException("Los indicadores no pueden ser negativos.");
        if (h < 0 || h > 100) throw new DomainException("El historial de pagos debe estar entre 0 y 100.");
        return nombres.Trim();
    }
    public static Solicitante Registrar(NumeroDocumento d, string n, Dinero i, Dinero g, Dinero o, int a, int ac, int h, int am, DateTimeOffset f) => new(Guid.NewGuid(), d, n, i, g, o, a, ac, h, am, EstadoRegistro.ACTIVO, f);
    public static Solicitante Reconstituir(Guid id, NumeroDocumento d, string n, Dinero i, Dinero g, Dinero o, int a, int ac, int h, int am, EstadoRegistro e, DateTimeOffset f) => new(id, d, n, i, g, o, a, ac, h, am, e, f);
    public void ActualizarPerfil(string n, Dinero i, Dinero g, Dinero o, int a, int ac, int h, int am)
    {
        NombresRazonSocial = Validar(n, i, g, o, a, ac, h, am);
        IngresosMensuales = i;
        GastosMensuales = g;
        ObligacionesFinancieras = o;
        AntiguedadLaboralNegocio = a;
        NumeroObligacionesActivas = ac;
        PuntajeHistorialPagos = h;
        AlertasMora = am;
    }
    public bool TieneIngresosValidos() => IngresosMensuales.Monto > 0; 
    public bool TieneDatosFinancierosCompletos() => IngresosMensuales is not null && GastosMensuales is not null && ObligacionesFinancieras is not null;
}
