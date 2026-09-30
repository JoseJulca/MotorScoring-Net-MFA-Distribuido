

using MotorScoring.Audit.Worker;
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IAuditoriaProcessor, JsonlAuditoriaProcessor>();
builder.Services.AddHostedService<KafkaConsumer>();
await builder.Build().RunAsync();
