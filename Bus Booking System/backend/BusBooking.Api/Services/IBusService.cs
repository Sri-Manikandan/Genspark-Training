using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IBusService
{
    Task<IReadOnlyList<BusDto>> ListForOperatorAsync(Guid operatorUserId, CancellationToken ct);
    Task<BusDto> CreateAsync(Guid operatorUserId, CreateBusRequest body, CancellationToken ct);
    Task<BusDto> UpdateOperationalStatusAsync(Guid operatorUserId, Guid busId, string newStatus, CancellationToken ct);
    Task<BusDto> RetireAsync(Guid operatorUserId, Guid busId, CancellationToken ct);

    Task<IReadOnlyList<BusDto>> ListByApprovalStatusAsync(string? status, CancellationToken ct);
    Task<BusDto> ApproveAsync(Guid adminId, Guid busId, CancellationToken ct);
    Task<BusDto> RejectAsync(Guid adminId, Guid busId, string reason, CancellationToken ct);
}
