using System.Text.RegularExpressions;
using MotorScoring.Domain.Enums;
using MotorScoring.Domain.Exceptions;
namespace MotorScoring.Domain.ValueObjects;

public sealed record Dinero
{
    public decimal Monto { get; }
    public Moneda Moneda { get; }
    public Dinero(decimal monto, Moneda moneda)
    {
        Monto = Math.Round(monto, 2, MidpointRounding.AwayFromZero);
        if (Monto < 0) throw new DomainException("El monto no puede ser negativo.");
        Moneda = moneda;
    }
    public static Dinero Positivo(decimal monto, Moneda moneda)
    {
        var d = new Dinero(monto, moneda);
        if (d.Monto <= 0) throw new DomainException("El monto debe ser mayor que cero.");
        return d;
    }
}
public sealed record NumeroDocumento
{
    public TipoDocumento Tipo { get; }
    public string Numero { get; }
    public NumeroDocumento(TipoDocumento tipo, string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            throw new DomainException("El número de documento es obligatorio.");

        Tipo = tipo;
        Numero = numero.Trim().ToUpperInvariant();

        bool ok = tipo switch
        {
            TipoDocumento.DNI => Regex.IsMatch(Numero, @"^\d{8}$"),
            TipoDocumento.RUC => Regex.IsMatch(Numero, @"^\d{11}$"),
            TipoDocumento.CE => Regex.IsMatch(Numero, @"^[A-Z0-9]{9,12}$"),
            TipoDocumento.PASAPORTE => Regex.IsMatch(Numero, @"^[A-Z0-9]{6,12}$"),
            _ => false
        };

        if (!ok)
            throw new DomainException($"Formato de documento inválido para {tipo}.");
    }
}
public sealed record IdentificadorExterno
{
    public string Valor { get; }
    public IdentificadorExterno(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new DomainException("El identificador externo es obligatorio.");
        Valor = valor.Trim();
        if (Valor.Length > 100) throw new DomainException("El identificador externo no puede superar 100 caracteres.");
    }
}
public sealed record Porcentaje
{
    public decimal Valor { get; }
    public Porcentaje(decimal valor)
    {
        Valor = Math.Round(valor, 4, MidpointRounding.AwayFromZero);
        if (Valor < 0 || Valor > 100) throw new DomainException("El porcentaje debe estar entre 0 y 100.");
    }
}
public sealed record PuntajeCrediticio
{
    public int Valor { get; }
    public PuntajeCrediticio(int valor)
    {
        if (valor is < 0 or > 1000) throw new PuntajeInvalidoException("El puntaje debe estar entre 0 y 1000.");
        Valor = valor;
    }
}
public sealed record CapacidadPago(Dinero Disponible)
{
    public static CapacidadPago Calcular(decimal ingresos, decimal gastos, decimal obligaciones, Moneda moneda)
        => new(new Dinero(Math.Max(0m, ingresos - gastos - obligaciones), moneda));
}
public sealed record RelacionDeudaIngreso
{
    public decimal Porcentaje { get; }
    public RelacionDeudaIngreso(decimal porcentaje)
    {
        if (porcentaje < 0) throw new DomainException("La relación deuda-ingreso no puede ser negativa.");
        Porcentaje = Math.Round(porcentaje, 4, MidpointRounding.AwayFromZero);
    }
    public static RelacionDeudaIngreso Calcular(decimal obligaciones, decimal ingresos)
    {
        if (ingresos <= 0) throw new DomainException("Los ingresos deben ser mayores que cero.");
        return new(Math.Round(obligaciones * 100m / ingresos, 4, MidpointRounding.AwayFromZero));
    }
}
public sealed record RelacionCuotaIngreso
{
    public decimal Porcentaje { get; }
    public RelacionCuotaIngreso(decimal porcentaje)
    {
        if (porcentaje < 0) throw new DomainException("La relación cuota-ingreso no puede ser negativa.");
        Porcentaje = Math.Round(porcentaje, 4, MidpointRounding.AwayFromZero);
    }
    public static RelacionCuotaIngreso Calcular(decimal monto, int plazo, decimal ingresos)
    {
        if (monto <= 0) throw new DomainException("El monto solicitado debe ser mayor que cero.");
        if (plazo <= 0) throw new DomainException("El plazo solicitado debe ser mayor que cero.");
        if (ingresos <= 0) throw new DomainException("Los ingresos deben ser mayores que cero.");
        var cuota = Math.Round(monto / plazo, 4, MidpointRounding.AwayFromZero);
        return new(Math.Round(cuota * 100m / ingresos, 4, MidpointRounding.AwayFromZero));
    }
}
