using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace MotorScoring.Api.Health;

public sealed class SqlServerHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct =
        default)
    {
        try
        {
            await using
            var c = new SqlConnection(configuration.GetConnectionString("MotorScoringDb"));
            await c.OpenAsync(ct);
            return HealthCheckResult.Healthy("SQL Server disponible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server no disponible", ex);
        }
    }
}