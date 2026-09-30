using MotorScoring.Domain.Entities;
using MotorScoring.Domain.Enums;
using MotorScoring.Domain.Services;
using MotorScoring.Domain.ValueObjects;
namespace MotorScoring.Domain.Tests;

using Xunit;

public sealed class CalculadorScoringTests
{
    [Fact]
    public void RelacionCuotaIngreso_Debe_Respetar_Formula()
    {
        var r = RelacionCuotaIngreso.Calcular(12000m, 12, 5000m);
        Assert.Equal(20m, r.Porcentaje);
    }

    [Fact]
    public void CapacidadPago_No_Debe_Ser_Negativa()
    {
        var c = CapacidadPago.Calcular(1000, 900, 500, Moneda.PEN);
        Assert.Equal(0m, c.Disponible.Monto);
    }

}
