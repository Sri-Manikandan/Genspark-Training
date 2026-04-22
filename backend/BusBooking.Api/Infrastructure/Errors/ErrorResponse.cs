namespace BusBooking.Api.Infrastructure.Errors;

public record ErrorEnvelope(string Code, string Message, string CorrelationId, object? Details = null);

public record ErrorResponse(ErrorEnvelope Error);
