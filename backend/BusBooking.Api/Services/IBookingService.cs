using BusBooking.Api.Dtos;

namespace BusBooking.Api.Services;

public interface IBookingService
{
    Task<CreateBookingResponseDto> CreateAsync(Guid userId, CreateBookingRequest req, CancellationToken ct);
    Task<BookingDetailDto> VerifyPaymentAsync(Guid userId, Guid bookingId, VerifyPaymentRequest req, CancellationToken ct);
    Task<BookingDetailDto> GetAsync(Guid userId, Guid bookingId, CancellationToken ct);
    Task<byte[]> GetTicketPdfAsync(Guid userId, Guid bookingId, CancellationToken ct);
}

