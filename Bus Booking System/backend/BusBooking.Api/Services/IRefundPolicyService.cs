namespace BusBooking.Api.Services;

public interface IRefundPolicyService
{
    RefundQuote Quote(decimal totalAmount, DateTime departureUtc, DateTime nowUtc);
}
