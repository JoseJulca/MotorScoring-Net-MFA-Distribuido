

using MotorScoring.Notification.Worker;
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<INotificacionProcessor, EmailSimuladoProcessor>();
builder.Services.AddHostedService<RabbitConsumer>();
await builder.Build().RunAsync();
