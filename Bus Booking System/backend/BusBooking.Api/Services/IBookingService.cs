using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IBookingService
{
    Task<CreateBookingResponseDto> CreateAsync(Guid userId, CreateBookingRequest req, CancellationToken ct);
    Task<BookingDetailDto> VerifyPaymentAsync(Guid userId, Guid bookingId, VerifyPaymentRequest req, CancellationToken ct);
    Task<BookingDetailDto> GetAsync(Guid userId, Guid bookingId, CancellationToken ct);
    Task<byte[]> GetTicketPdfAsync(Guid userId, Guid bookingId, CancellationToken ct);
    Task<BookingListResponseDto> ListAsync(
        Guid userId,
        string filter,
        int page,
        int pageSize,
        CancellationToken ct);
    Task<RefundPreviewDto> GetRefundPreviewAsync(Guid userId, Guid bookingId, CancellationToken ct);
    Task<BookingDetailDto> CancelAsync(Guid userId, Guid bookingId, CancelBookingRequest req, CancellationToken ct);
}

