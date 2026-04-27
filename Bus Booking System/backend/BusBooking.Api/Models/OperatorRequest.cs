namespace BusBooking.Api.Models;

public class OperatorRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Status { get; set; }
    public required string CompanyName { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? RejectReason { get; set; }

    public User? User { get; set; }
}
