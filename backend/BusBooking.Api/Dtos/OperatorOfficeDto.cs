namespace BusBooking.Api.Dtos;

public record OperatorOfficeDto(
    Guid Id,
    Guid CityId,
    string CityName,
    string AddressLine,
    string Phone,
    bool IsActive);
