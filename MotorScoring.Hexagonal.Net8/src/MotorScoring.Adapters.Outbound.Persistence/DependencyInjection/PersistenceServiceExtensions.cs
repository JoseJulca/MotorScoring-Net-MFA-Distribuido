using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MotorScoring.Adapters.Outbound.Persistence.Context;
using MotorScoring.Adapters.Outbound.Persistence.Repositories;
using MotorScoring.Application.Ports.Out;
namespace MotorScoring.Adapters.Outbound.Persistence.DependencyInjection;

public static class PersistenceServiceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection s, IConfiguration c)
    {
        var cs = c.GetConnectionString("MotorScoringDb") ??
            throw new InvalidOperationException("ConnectionStrings:MotorScoringDb es obligatoria.");
        s.AddDbContext<MotorScoringDbContext>(o => o.UseSqlServer(cs));
        s.AddScoped<ISolicitanteRepository, SolicitanteRepository>();
        s.AddScoped<ISolicitudCreditoRepository, SolicitudCreditoRepository>();
        s.AddScoped<IProductoCrediticioRepository, ProductoCrediticioRepository>();
        s.AddScoped<IModeloScoringRepository, ModeloScoringRepository>();
        s.AddScoped<IEvaluacionCrediticiaRepository, EvaluacionCrediticiaRepository>();
        s.AddScoped<IUnitOfWork, UnitOfWork>();
        return s;
    }
}