namespace BusBooking.Api.Dtos;

public record BusDto(
    Guid Id,
    Guid OperatorUserId,
    string RegistrationNumber,
    string BusName,
    string BusType,
    int Capacity,
    string ApprovalStatus,
    string OperationalStatus,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    string? RejectReason);
