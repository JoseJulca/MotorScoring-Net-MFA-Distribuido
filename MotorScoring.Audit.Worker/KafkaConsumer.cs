using System.Text.Json;
using Confluent.Kafka;

using MotorScoring.Audit.Worker.Models;
namespace MotorScoring.Audit.Worker;
public sealed class KafkaConsumer(IConfiguration config, IAuditoriaProcessor processor, ILogger<KafkaConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string,string>(new ConsumerConfig { BootstrapServers = config["Kafka:BootstrapServers"] ?? "kafka:9092", GroupId = "scoring-auditoria-worker", AutoOffsetReset = AutoOffsetReset.Earliest, EnableAutoCommit = false, EnableAutoOffsetStore = false }).Build();
                consumer.Subscribe("scoring.auditoria");
                while (!ct.IsCancellationRequested)
                {
                    var message = consumer.Consume(ct);
                    var evento = JsonSerializer.Deserialize<EvaluacionCompletada>(message.Message.Value) ?? throw new JsonException("Evento vacío");
                    await processor.ProcesarAsync(evento, ct);
                    consumer.StoreOffset(message);
                    consumer.Commit(message);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Kafka/consumo falló; reintentando"); await Task.Delay(5000, ct); }
        }
    }
}
