using BusBooking.Api.Infrastructure.RefundPolicy;
using Microsoft.Extensions.Options;

namespace BusBooking.Api.Services;

public class RefundPolicyService : IRefundPolicyService
{
    private readonly RefundPolicyOptions _options;

    public RefundPolicyService(IOptions<RefundPolicyOptions> options)
    {
        _options = options.Value;
    }

    public RefundQuote Quote(decimal totalAmount, DateTime departureUtc, DateTime nowUtc)
    {
        if (departureUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateTime must have Kind = Utc.", nameof(departureUtc));
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateTime must have Kind = Utc.", nameof(nowUtc));

        var hours = (departureUtc - nowUtc).TotalHours;

        if (hours < _options.BlockBelowHours)
            return new RefundQuote(0, 0m, hours, Blocked: true);

        var tier = _options.Tiers
            .OrderByDescending(t => t.MinHoursBeforeDeparture)
            .FirstOrDefault(t => hours >= t.MinHoursBeforeDeparture);

        if (tier is null)
            return new RefundQuote(0, 0m, hours, Blocked: false);

        var amount = Math.Round(totalAmount * tier.RefundPercent / 100m, 2);
        return new RefundQuote(tier.RefundPercent, amount, hours, Blocked: false);
    }
}
