using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IOperatorRequestService
{
    Task<OperatorRequestDto> SubmitAsync(Guid userId, BecomeOperatorRequest body, CancellationToken ct);
    Task<IReadOnlyList<OperatorRequestDto>> ListAsync(string? status, CancellationToken ct);
    Task<OperatorRequestDto> ApproveAsync(Guid adminId, Guid requestId, CancellationToken ct);
    Task<OperatorRequestDto> RejectAsync(Guid adminId, Guid requestId, string reason, CancellationToken ct);
}
