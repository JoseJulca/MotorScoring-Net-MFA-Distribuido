using MotorScoring.Domain.Enums;
using MotorScoring.Domain.Exceptions;
using MotorScoring.Domain.ValueObjects;
namespace MotorScoring.Domain.Entities;

public sealed class ProductoCrediticio
{
    public Guid Id { get; }
    public string Codigo { get; }
    public string Nombre { get; }
    public Dinero MontoMinimo { get; }
    public Dinero MontoMaximo { get; }
    public int PlazoMinimo { get; }
    public int PlazoMaximo { get; }
    public EstadoProducto Estado { get; }
    public Guid IdModeloScoring { get; }
    public ProductoCrediticio(Guid id, string codigo, string nombre, Dinero min, Dinero max, int pmin, int pmax, EstadoProducto estado, Guid modelo) 
    { 
        if (min.Moneda != max.Moneda || min.Monto > max.Monto) 
            throw new DomainException("Rango monetario inválido."); 
        if (pmin <= 0 || pmax < pmin) 
            throw new DomainException("Rango de plazo inválido."); 
        Id = id; 
        Codigo = codigo; 
        Nombre = nombre; 
        MontoMinimo = min; 
        MontoMaximo = max; 
        PlazoMinimo = pmin; 
        PlazoMaximo = pmax; 
        Estado = estado; 
        IdModeloScoring = modelo; 
    }
    public void ValidarSolicitud(Dinero monto, int plazo) 
    { 
        if (Estado != EstadoProducto.ACTIVO) 
            throw new SolicitudNoEvaluableException("El producto está inactivo."); 
        if (monto.Moneda != MontoMinimo.Moneda) 
            throw new SolicitudNoEvaluableException("La moneda no corresponde al producto."); 
        if (monto.Monto < MontoMinimo.Monto || monto.Monto > MontoMaximo.Monto) 
            throw new SolicitudNoEvaluableException("El monto está fuera de los límites del producto."); 
        if (plazo < PlazoMinimo || plazo > PlazoMaximo) 
            throw new SolicitudNoEvaluableException("El plazo está fuera de los límites del producto."); 
    }
}
public sealed class ReglaEvaluacion
{
    public Guid Id { get; }
    public Guid IdFactor { get; }
    public string Codigo { get; }
    public string Descripcion { get; }
    public decimal ValorMinimo { get; }
    public decimal ValorMaximo { get; }
    public int Puntaje { get; }
    public bool Excluyente { get; }
    public ResultadoScoring? ResultadoExcluyente { get; }
    public EstadoRegla Estado { get; }
    public ReglaEvaluacion(Guid id, Guid factor, string codigo, string descripcion, decimal min, decimal max, int puntaje, bool excluyente, ResultadoScoring? resultado, EstadoRegla estado) 
    { 
        Id = id; 
        IdFactor = factor; 
        Codigo = codigo; 
        Descripcion = descripcion; 
        ValorMinimo = min; 
        ValorMaximo = max;
        Puntaje = puntaje; 
        Excluyente = excluyente; 
        ResultadoExcluyente = resultado;
        Estado = estado; 
    }
    public bool Cumple(decimal valor) => Estado == EstadoRegla.ACTIVA && valor >= ValorMinimo && valor <= ValorMaximo;
}
public sealed class FactorScoring
{
    public Guid Id { get; }
    public Guid IdVersionModelo { get; }
    public string Codigo { get; }
    public string Nombre { get; }
    public string? Descripcion { get; }
    public Porcentaje Peso { get; }
    public EstadoFactor Estado { get; }
    public IReadOnlyList<ReglaEvaluacion> Reglas { get; }
    public FactorScoring(Guid id, Guid version, string codigo, string nombre, string? descripcion, Porcentaje peso, EstadoFactor estado, IEnumerable<ReglaEvaluacion> reglas) 
    { 
        Id = id; 
        IdVersionModelo = version; 
        Codigo = codigo; 
        Nombre = nombre; 
        Descripcion = descripcion; 
        Peso = peso; 
        Estado = estado; 
        Reglas = reglas.ToList().AsReadOnly();
    }
    public ReglaEvaluacion Evaluar(decimal valor) => Reglas.FirstOrDefault(r => r.Cumple(valor)) ?? throw new SolicitudNoEvaluableException($"No existe regla aplicable para {Codigo}: {valor}.");
}
public sealed class VersionModelo
{
    public Guid Id { get; }
    public Guid IdModelo { get; }
    public string NumeroVersion { get; }
    public DateOnly FechaInicioVigencia { get; }
    public DateOnly? FechaFinVigencia { get; }
    public EstadoVersionModelo Estado { get; }
    public IReadOnlyList<FactorScoring> Factores { get; }
    public VersionModelo(Guid id, Guid modelo, string numero, DateOnly ini, DateOnly? fin, EstadoVersionModelo estado, IEnumerable<FactorScoring> factores) 
    { 
        Id = id; 
        IdModelo = modelo; 
        NumeroVersion = numero; 
        FechaInicioVigencia = ini; 
        FechaFinVigencia = fin; 
        Estado = estado; 
        Factores = factores.ToList().AsReadOnly(); 
    }
    public bool EstaVigente(DateOnly fecha) => Estado == EstadoVersionModelo.ACTIVA && fecha >= FechaInicioVigencia && (!FechaFinVigencia.HasValue || fecha <= FechaFinVigencia.Value);
    public void ValidarPesos() 
    { 
        var total = Factores.Where(f => f.Estado == EstadoFactor.ACTIVO).Sum(f => f.Peso.Valor); 
        if (total != 100m) 
            throw new SolicitudNoEvaluableException($"La suma de pesos activos debe ser 100 y es {total}."); 
    }
}
public sealed class ModeloScoring
{
    public Guid Id { get; }
    public string Codigo { get; }
    public string Nombre { get; }
    public string? Descripcion { get; }
    public EstadoModelo Estado { get; }
    public DateTimeOffset FechaCreacion { get; }
    public IReadOnlyList<VersionModelo> Versiones { get; }
    public ModeloScoring(Guid id, string codigo, string nombre, string? descripcion, EstadoModelo estado, DateTimeOffset fecha, IEnumerable<VersionModelo> versiones) 
    { 
        Id = id; 
        Codigo = codigo; 
        Nombre = nombre; 
        Descripcion = descripcion; 
        Estado = estado; 
        FechaCreacion = fecha; 
        Versiones = versiones.ToList().AsReadOnly(); 
    }
    public VersionModelo VersionActiva(DateOnly fecha) 
    { 
        if (Estado != EstadoModelo.ACTIVO) 
            throw new ModeloActivoNoEncontradoException("El modelo está inactivo."); 
        return Versiones.FirstOrDefault(v => v.EstaVigente(fecha)) ?? throw new ModeloActivoNoEncontradoException("No existe una versión activa y vigente del modelo."); 
    }
}
