using System.Text.Json;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Models;

namespace BusBooking.Api.Services;

public class AuditLogWriter : IAuditLogWriter
{
    private readonly AppDbContext _db;

    public AuditLogWriter(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(
        Guid actorUserId,
        string action,
        string targetType,
        Guid targetId,
        object? metadata = null,
        CancellationToken ct = default)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLog.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
