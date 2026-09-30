using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MotorScoring.Adapters.Inbound.Api.DependencyInjection;
using MotorScoring.Adapters.Inbound.Api.Middleware;
using MotorScoring.Adapters.Outbound.Persistence.DependencyInjection;
using MotorScoring.Api.DependencyInjection;
using MotorScoring.Api.Database;
using MotorScoring.Api.Health;
using MotorScoring.Api.Integration;
using MotorScoring.Application.Ports.Out;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInboundApi();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddHttpClient<IEvaluacionNotificacionPort, NotificacionesClient>(client => client.BaseAddress = new Uri(builder.Configuration["Notificaciones:BaseUrl"] ?? "http://motor-scoring-notificaciones-api:8083/"));

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer requerido.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience requerido.");
var jwtKey = builder.Configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey requerido.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Scoring.Solicitud.Crear", p => p.RequireClaim("permission", "Scoring.Solicitud.Crear"));
    options.AddPolicy("Scoring.Evaluacion.Ejecutar", p => p.RequireClaim("permission", "Scoring.Evaluacion.Ejecutar"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddCheck<SqlServerHealthCheck>("sqlserver");

var app = builder.Build();
var cs = builder.Configuration.GetConnectionString("MotorScoringDb") ?? throw new InvalidOperationException("Connection string requerida.");
DatabaseMigrationRunner.Run(cs, app.Logger);
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
public partial class Program { }
