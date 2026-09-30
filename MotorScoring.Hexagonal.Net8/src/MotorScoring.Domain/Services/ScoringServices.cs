using MotorScoring.Domain.Entities;
using MotorScoring.Domain.Enums;
using MotorScoring.Domain.Exceptions;
using MotorScoring.Domain.ValueObjects;
namespace MotorScoring.Domain.Services;

public sealed class CalculadorCapacidadPago
{
    public CapacidadPago Calcular(Solicitante s) => CapacidadPago.Calcular(s.IngresosMensuales.Monto, s.GastosMensuales.Monto, s.ObligacionesFinancieras.Monto, s.Moneda);
}
public sealed class CalculadorRelacionDeudaIngreso
{
    public RelacionDeudaIngreso Calcular(Solicitante s) => RelacionDeudaIngreso.Calcular(s.ObligacionesFinancieras.Monto, s.IngresosMensuales.Monto);
}
public sealed class CalculadorRelacionCuotaIngreso
{
    public RelacionCuotaIngreso Calcular(SolicitudCredito solicitud, Solicitante s) => RelacionCuotaIngreso.Calcular(solicitud.MontoSolicitado.Monto, solicitud.PlazoSolicitado, s.IngresosMensuales.Monto);
}
public sealed class EvaluadorReglasExcluyentes
{
    public ResultadoScoring? Evaluar(IEnumerable<ResultadoFactor> r) => r.FirstOrDefault(x => x.ReglaExcluyente)?.ResultadoExcluyente;
}
public sealed class CalculadorScoring
{
    private readonly CalculadorCapacidadPago _capacidad;
    private readonly CalculadorRelacionDeudaIngreso _relacion;
    private readonly CalculadorRelacionCuotaIngreso _rci;
    private readonly EvaluadorReglasExcluyentes _excluyentes;
    public CalculadorScoring(CalculadorCapacidadPago c, CalculadorRelacionDeudaIngreso r, CalculadorRelacionCuotaIngreso rci, EvaluadorReglasExcluyentes e)
    {
        _capacidad = c;
        _relacion = r;
        _rci = rci;
        _excluyentes = e;
    }
    public EvaluacionCrediticia Calcular(SolicitudCredito solicitud, Solicitante solicitante, VersionModelo version, DateTimeOffset fecha)
    {
        if (!solicitud.EstaEvaluable()) throw new SolicitudNoEvaluableException("La solicitud no está REGISTRADA.");
        version.ValidarPesos();
        var cp = _capacidad.Calcular(solicitante);
        var rdi = _relacion.Calcular(solicitante);
        var rci = _rci.Calcular(solicitud, solicitante);
        var resultados = new List<ResultadoFactor>();
        int total = 0;
        foreach (var f in version.Factores)
        {
            if (f.Estado != EstadoFactor.ACTIVO) continue;
            decimal valor = ValorFactor(f.Codigo, solicitud, solicitante, cp, rdi, rci);
            var regla = f.Evaluar(valor);
            int aporte = regla.Excluyente ? 0 : RoundHalfUp(regla.Puntaje * f.Peso.Valor / 10m);
            total += aporte;
            resultados.Add(new(f.Id, f.Codigo, valor, f.Peso.Valor, regla.Puntaje, aporte, regla.Codigo, regla.Descripcion, regla.Excluyente, regla.ResultadoExcluyente));
        }
        total = Math.Clamp(total, 0, 1000);
        var resultado = _excluyentes.Evaluar(resultados) ?? Clasificar(total);
        var estado = resultados.Any(r => r.ReglaExcluyente) ? EstadoEvaluacion.CON_REGLA_EXCLUYENTE : EstadoEvaluacion.COMPLETADA;
        return EvaluacionCrediticia.Crear(solicitud.Id, version.Id, fecha, new PuntajeCrediticio(total), resultado, estado, resultados);
    }
    private static int RoundHalfUp(decimal v) => (int)Math.Round(v, 0, MidpointRounding.AwayFromZero);
    private static ResultadoScoring Clasificar(int p) => p >= 750 ? ResultadoScoring.PREAPROBADA : p >= 600 ? ResultadoScoring.REVISION_MANUAL : ResultadoScoring.RECHAZADA;
    private static decimal Porcentaje(decimal n, decimal d) => d <= 0 ? 9999.0000m : Math.Round(n* 100m / d, 4, MidpointRounding.AwayFromZero);
	private static decimal ValorFactor(string codigo, SolicitudCredito sol, Solicitante s, CapacidadPago cp, RelacionDeudaIngreso rdi, RelacionCuotaIngreso rci) => codigo
    switch
    {
        "HISTORIAL_PAGOS" => s.PuntajeHistorialPagos,
        "RELACION_DEUDA_INGRESO" => rdi.Porcentaje,
        "CAPACIDAD_PAGO" => Porcentaje(cp.Disponible.Monto, s.IngresosMensuales.Monto),
        "ESTABILIDAD_INGRESOS"
        or "ANTIGUEDAD_LABORAL" => s.AntiguedadLaboralNegocio,
        "OBLIGACIONES_ACTIVAS" => s.NumeroObligacionesActivas,
        "MONTO_CAPACIDAD" => Porcentaje(sol.MontoSolicitado.Monto, cp.Disponible.Monto * sol.PlazoSolicitado),
        "ALERTAS_MORA" => s.AlertasMora,
        "RELACION_CUOTA_INGRESO" => rci.Porcentaje,
        _ =>
            throw new SolicitudNoEvaluableException("Factor no soportado: " + codigo)
    };
}