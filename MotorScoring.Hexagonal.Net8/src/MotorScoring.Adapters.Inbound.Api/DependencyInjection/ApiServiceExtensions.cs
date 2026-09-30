using Microsoft.Extensions.DependencyInjection;
namespace MotorScoring.Adapters.Inbound.Api.DependencyInjection;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddInboundApi(this IServiceCollection s)
    {
        s.AddControllers().AddApplicationPart(typeof(ApiServiceExtensions).Assembly);
        return s;
    }
}