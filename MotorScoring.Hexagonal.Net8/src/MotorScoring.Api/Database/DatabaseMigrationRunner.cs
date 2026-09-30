using DbUp;
namespace MotorScoring.Api.Database;

public static class DatabaseMigrationRunner
{
    public static void Run(string connectionString, ILogger logger)
    {
        EnsureDatabase.For.SqlDatabase(connectionString);
        var upgrader = DeployChanges.To.SqlDatabase(connectionString).WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrationRunner).Assembly).LogToAutodetectedLog().Build();
        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            logger.LogError(result.Error, "DbUp falló");
            throw result.Error;
        }
        logger.LogInformation("DbUp completado correctamente.");
    }
}