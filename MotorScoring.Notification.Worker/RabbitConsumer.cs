using System.Text;
using System.Text.Json;

using MotorScoring.Notification.Worker.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
namespace MotorScoring.Notification.Worker;
public sealed class RabbitConsumer(IConfiguration config, INotificacionProcessor processor, ILogger<RabbitConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = config["RabbitMq:Host"] ?? "rabbitmq", UserName = config["RabbitMq:User"] ?? "guest", Password = config["RabbitMq:Password"] ?? "guest", DispatchConsumersAsync = true };
                using var connection = factory.CreateConnection(); using var channel = connection.CreateModel();
                channel.ExchangeDeclare("scoring.exchange", ExchangeType.Direct, durable: true);
                channel.QueueDeclare("scoring.notificaciones.email.dlq", durable: true, exclusive: false, autoDelete: false);
        channel.QueueDeclare("scoring.notificaciones.email", durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object> { ["x-dead-letter-exchange"] = "", ["x-dead-letter-routing-key"] = "scoring.notificaciones.email.dlq" });
                channel.QueueBind("scoring.notificaciones.email", "scoring.exchange", "scoring.notificacion.email");
                channel.BasicQos(0, 1, false);
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (_, args) =>
                {
                    try
                    {
                        var evento = JsonSerializer.Deserialize<EvaluacionCompletada>(Encoding.UTF8.GetString(args.Body.ToArray())) ?? throw new JsonException("Evento vacío");
                        await processor.ProcesarAsync(evento, stoppingToken);
                        channel.BasicAck(args.DeliveryTag, false);
                    }
                    catch (Exception ex) { logger.LogError(ex, "Error procesando notificación"); channel.BasicNack(args.DeliveryTag, false, requeue: false); }
                };
                channel.BasicConsume("scoring.notificaciones.email", false, consumer);
                while (!stoppingToken.IsCancellationRequested && connection.IsOpen) await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "RabbitMQ no disponible; reconectando"); await Task.Delay(5000, stoppingToken); }
        }
    }
}
