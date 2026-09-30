using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MotorScoring.Notificaciones.Application.Ports;
using MotorScoring.Notificaciones.Application.Contracts;
using RabbitMQ.Client;
namespace MotorScoring.Notificaciones.Infrastructure.Messaging;
public sealed class RabbitMqPublisher(IConfiguration config) : INotificacionPublisher
{
    public Task PublicarAsync(EvaluacionCompletada evento, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var factory = new ConnectionFactory { HostName = config["RabbitMq:Host"] ?? "rabbitmq", UserName = config["RabbitMq:User"] ?? "guest", Password = config["RabbitMq:Password"] ?? "guest", DispatchConsumersAsync = true };
        using var connection = factory.CreateConnection(); using var channel = connection.CreateModel();
        channel.ExchangeDeclare("scoring.exchange", ExchangeType.Direct, durable: true);
        channel.QueueDeclare("scoring.notificaciones.email.dlq", durable: true, exclusive: false, autoDelete: false);
        channel.QueueDeclare("scoring.notificaciones.email", durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object> { ["x-dead-letter-exchange"] = "", ["x-dead-letter-routing-key"] = "scoring.notificaciones.email.dlq" });
        channel.QueueBind("scoring.notificaciones.email", "scoring.exchange", "scoring.notificacion.email");
        channel.ConfirmSelect();
        var properties = channel.CreateBasicProperties(); properties.Persistent = true; properties.MessageId = evento.EventId.ToString();
        channel.BasicPublish("scoring.exchange", "scoring.notificacion.email", properties, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento)));
        if (!channel.WaitForConfirms(TimeSpan.FromSeconds(10))) throw new IOException("RabbitMQ no confirmó la publicación.");
        return Task.CompletedTask;
    }
}
