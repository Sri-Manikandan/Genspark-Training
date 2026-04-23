namespace BusBooking.Api.Dtos;

public record OperatorRevenueItemDto(
    Guid BusId,
    string BusName,
    string RegistrationNumber,
    int ConfirmedBookings,
    int TotalSeats,
    decimal TotalFare);
