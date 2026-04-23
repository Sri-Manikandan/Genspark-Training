namespace BusBooking.Api.Services;

public interface IAuditLogWriter
{
    Task WriteAsync(
        Guid actorUserId,
        string action,
        string targetType,
        Guid targetId,
        object? metadata = null,
        CancellationToken ct = default);
}
