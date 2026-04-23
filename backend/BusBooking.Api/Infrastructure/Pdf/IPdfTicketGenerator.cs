using BusBooking.Api.Dtos;

namespace BusBooking.Api.Infrastructure.Pdf;

public interface IPdfTicketGenerator
{
    byte[] Generate(BookingDetailDto booking);
}

