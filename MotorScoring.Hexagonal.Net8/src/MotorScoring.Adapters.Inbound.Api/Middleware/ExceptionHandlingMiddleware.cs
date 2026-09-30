using System.Text.Json;
using MotorScoring.Adapters.Inbound.Api.Contracts;
using MotorScoring.Application.Exceptions;
using MotorScoring.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
namespace MotorScoring.Adapters.Inbound.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando {Path}", ctx.Request.Path);
            var (status, code, msg) = ex
            switch
            {
                RecursoNoEncontradoException => (404, "RESOURCE_NOT_FOUND", ex.Message),
                SolicitudDuplicadaException => (409, "DUPLICATE_APPLICATION", ex.Message),
                DomainException => (422, "BUSINESS_VALIDATION_ERROR", ex.Message),
                ArgumentException => (400, "INVALID_FORMAT", "Formato de solicitud inválido."),
                _ => (500, "INTERNAL_ERROR", "Ocurrió un error interno.")
            };
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(ctx.Response.Body, new ErrorResponse(DateTimeOffset.UtcNow, status, code, msg, ctx.Request.Path));
        }
    }
}
