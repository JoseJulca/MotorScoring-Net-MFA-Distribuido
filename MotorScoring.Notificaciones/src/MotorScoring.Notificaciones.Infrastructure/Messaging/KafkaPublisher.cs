using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using MotorScoring.Notificaciones.Application.Ports;
using MotorScoring.Notificaciones.Application.Contracts;
namespace MotorScoring.Notificaciones.Infrastructure.Messaging;
public sealed class KafkaPublisher(IConfiguration config) : IAuditoriaPublisher
{
    public async Task PublicarAsync(EvaluacionCompletada evento, CancellationToken ct)
    {
        using var producer = new ProducerBuilder<string,string>(new ProducerConfig { BootstrapServers = config["Kafka:BootstrapServers"] ?? "kafka:9092", Acks = Acks.All, EnableIdempotence = true }).Build();
        await producer.ProduceAsync("scoring.auditoria", new Message<string,string> { Key = evento.IdSolicitud.ToString(), Value = JsonSerializer.Serialize(evento) }, ct);
    }
}
