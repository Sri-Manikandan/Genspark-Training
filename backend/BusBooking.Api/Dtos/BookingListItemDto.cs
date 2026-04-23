namespace BusBooking.Api.Dtos;

public record BookingListItemDto(
    Guid BookingId,
    string BookingCode,
    Guid TripId,
    DateOnly TripDate,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    string SourceCity,
    string DestinationCity,
    string BusName,
    string OperatorName,
    int SeatCount,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? CancelledAt,
    decimal? RefundAmount,
    string? RefundStatus);
