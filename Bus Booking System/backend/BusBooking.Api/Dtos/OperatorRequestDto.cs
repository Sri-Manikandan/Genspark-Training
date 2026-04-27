namespace BusBooking.Api.Dtos;

public record OperatorRequestDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    string CompanyName,
    string Status,
    DateTime RequestedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedByAdminId,
    string? RejectReason);
