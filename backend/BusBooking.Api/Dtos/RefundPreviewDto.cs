namespace BusBooking.Api.Dtos;

public record RefundPreviewDto(
    Guid BookingId,
    decimal TotalAmount,
    int RefundPercent,
    decimal RefundAmount,
    double HoursUntilDeparture,
    bool Cancellable,
    string? BlockReason);
