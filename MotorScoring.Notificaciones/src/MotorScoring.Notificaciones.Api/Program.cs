using MotorScoring.Notificaciones.Application.Ports;
using MotorScoring.Notificaciones.Application.UseCases;
using MotorScoring.Notificaciones.Application.Contracts;
using MotorScoring.Notificaciones.Infrastructure.Messaging;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<INotificacionPublisher, RabbitMqPublisher>();
builder.Services.AddScoped<IAuditoriaPublisher, KafkaPublisher>();
builder.Services.AddScoped<PublicarEvaluacion>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/api/v1/notificaciones/evaluacion", async (EvaluacionCompletada evento, PublicarEvaluacion caso, HttpContext http, IConfiguration config, CancellationToken ct) =>
{
    var expected = config["InternalApi:Key"];
    if (string.IsNullOrWhiteSpace(expected) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(http.Request.Headers["X-Internal-Api-Key"].ToString()), System.Text.Encoding.UTF8.GetBytes(expected))) return Results.Unauthorized();
    try { await caso.ExecuteAsync(evento, ct); return Results.Accepted(value: new { evento.EventId }); }
    catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
});
app.Run();
