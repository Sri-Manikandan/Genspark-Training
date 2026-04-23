namespace BusBooking.Api.Dtos;

public record OperatorBookingListItemDto(
    Guid BookingId,
    string BookingCode,
    Guid TripId,
    DateOnly TripDate,
    string SourceCity,
    string DestinationCity,
    Guid BusId,
    string BusName,
    string CustomerName,
    int SeatCount,
    decimal TotalFare,
    decimal PlatformFee,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt);
