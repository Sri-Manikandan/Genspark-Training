namespace BusBooking.Api.Models;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
