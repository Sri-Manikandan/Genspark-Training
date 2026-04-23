namespace BusBooking.Api.Models;

public class Bus
{
    public Guid Id { get; set; }
    public Guid OperatorUserId { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string BusName { get; set; }
    public required string BusType { get; set; }
    public int Capacity { get; set; }
    public string ApprovalStatus { get; set; } = Models.BusApprovalStatus.Pending;
    public string OperationalStatus { get; set; } = Models.BusOperationalStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByAdminId { get; set; }
    public string? RejectReason { get; set; }

    public User? Operator { get; set; }
    public ICollection<SeatDefinition> Seats { get; set; } = new List<SeatDefinition>();
}
