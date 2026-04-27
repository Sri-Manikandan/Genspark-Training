namespace BusBooking.Api.Services;

public record RefundQuote(
    int RefundPercent,
    decimal RefundAmount,
    double HoursUntilDeparture,
    bool Blocked);
