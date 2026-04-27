using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IAdminOperatorService
{
    Task<IReadOnlyList<AdminOperatorListItemDto>> ListAsync(CancellationToken ct);
    Task<AdminOperatorListItemDto> DisableAsync(
        Guid adminId, Guid operatorUserId, string? reason, CancellationToken ct);
    Task<AdminOperatorListItemDto> EnableAsync(
        Guid adminId, Guid operatorUserId, CancellationToken ct);
}
