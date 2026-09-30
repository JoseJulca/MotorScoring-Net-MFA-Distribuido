using System.Text.Json;
using MotorScoring.Audit.Worker.Models;
namespace MotorScoring.Audit.Worker;
public interface IAuditoriaProcessor { Task ProcesarAsync(EvaluacionCompletada evento, CancellationToken ct); }
public sealed class JsonlAuditoriaProcessor(IConfiguration config, ILogger<JsonlAuditoriaProcessor> logger) : IAuditoriaProcessor
{
    public async Task ProcesarAsync(EvaluacionCompletada e, CancellationToken ct)
    {
        var path = config["Audit:FilePath"] ?? "/app/audit/auditoria-scoring.jsonl";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.AppendAllTextAsync(path, JsonSerializer.Serialize(e) + Environment.NewLine, ct);
        logger.LogInformation("[KAFKA][AUDITORIA] Evento {Evento} registrado para solicitud {Solicitud}", e.EventId, e.IdSolicitud);
    }
}
