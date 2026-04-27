namespace BusBooking.Api.Dtos;

public record AdminBookingListItemDto(
    Guid BookingId,
    string BookingCode,
    Guid TripId,
    DateOnly TripDate,
    string SourceCity,
    string DestinationCity,
    Guid BusId,
    string BusName,
    Guid OperatorUserId,
    string OperatorName,
    string CustomerName,
    string CustomerEmail,
    int SeatCount,
    decimal TotalFare,
    decimal PlatformFee,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt);
