using MotorScoring.Identity.Adapters.Outbound.Identity;
using MotorScoring.Identity.Application.Ports.In;
using MotorScoring.Identity.Application.UseCases;
using MotorScoring.Identity.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAuthUseCase, AuthUseCase>();
builder.Services.AddScoped<IUserAdministrationUseCase, UserAdministrationUseCase>();
builder.Services.AddIdentityOutboundAdapter(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddControllers().AddApplicationPart(typeof(MotorScoring.Identity.Controllers.AuthController).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();
await IdentitySeeder.SeedAsync(app.Services, app.Configuration);

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
