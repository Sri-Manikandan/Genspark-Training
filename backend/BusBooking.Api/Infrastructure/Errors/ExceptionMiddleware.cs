using System.Text.Json;
using FluentValidation;

namespace BusBooking.Api.Infrastructure.Errors;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await Handle(context, ex);
        }
    }

    private async Task Handle(HttpContext context, Exception ex)
    {
        var correlationId = context.TraceIdentifier;
        ErrorEnvelope envelope;
        int status;

        switch (ex)
        {
            case ValidationException vex:
                status = 400;
                var details = vex.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage });
                envelope = new ErrorEnvelope("VALIDATION_ERROR", "Request validation failed", correlationId, details);
                logger.LogWarning(ex, "Validation failed {CorrelationId}", correlationId);
                break;
            case AppException aex:
                status = aex.HttpStatus;
                envelope = new ErrorEnvelope(aex.Code, aex.Message, correlationId, aex.Details);
                logger.LogWarning(ex, "App exception {Code} {CorrelationId}", aex.Code, correlationId);
                break;
            default:
                status = 500;
                envelope = new ErrorEnvelope("INTERNAL_ERROR", "Something went wrong", correlationId);
                logger.LogError(ex, "Unhandled exception {CorrelationId}", correlationId);
                break;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(
            new ErrorResponse(envelope),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
