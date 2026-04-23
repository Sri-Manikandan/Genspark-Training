using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IOperatorOfficeService
{
    Task<IReadOnlyList<OperatorOfficeDto>> ListAsync(Guid operatorUserId, CancellationToken ct);
    Task<OperatorOfficeDto> CreateAsync(Guid operatorUserId, CreateOperatorOfficeRequest body, CancellationToken ct);
    Task DeleteAsync(Guid operatorUserId, Guid officeId, CancellationToken ct);
}
