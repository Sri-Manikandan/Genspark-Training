namespace BusBooking.Api.Dtos;

public record BookingDetailDto(
    Guid BookingId,
    string BookingCode,
    Guid TripId,
    DateOnly TripDate,
    string SourceCity,
    string DestinationCity,
    string BusName,
    string OperatorName,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal TotalFare,
    decimal PlatformFee,
    decimal TotalAmount,
    int SeatCount,
    string Status,
    DateTime? ConfirmedAt,
    DateTime CreatedAt,
    IReadOnlyList<BookingSeatDto> Seats);

