using Microsoft.Extensions.DependencyInjection;

using MotorScoring.Application.Ports.In;

using MotorScoring.Application.UseCases;

using MotorScoring.Domain.Services;

namespace MotorScoring.Api.DependencyInjection;


public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection s)
    {
        s.AddSingleton(TimeProvider.System);
        s.AddScoped<CalculadorCapacidadPago>();
        s.AddScoped<CalculadorRelacionDeudaIngreso>();
        s.AddScoped<CalculadorRelacionCuotaIngreso>();
        s.AddScoped<EvaluadorReglasExcluyentes>();
        s.AddScoped<CalculadorScoring>();
        s.AddScoped<IRegistrarSolicitudUseCase, RegistrarSolicitudUseCase>();
        s.AddScoped<IEvaluarScoringUseCase, EvaluarScoringUseCase>();
        return s;
    }
}

