namespace BusBooking.Api.Dtos;

public record BookingListItemDto(
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
    int SeatCount,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? CancelledAt,
    decimal? RefundAmount,
    string? RefundStatus);
