using BusBooking.Api.Infrastructure.RefundPolicy;
using BusBooking.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BusBooking.Api.Tests.Unit;

public class RefundPolicyServiceTests
{
    private static RefundPolicyService Build()
    {
        var options = Options.Create(new RefundPolicyOptions
        {
            Tiers =
            [
                new RefundPolicyTier { MinHoursBeforeDeparture = 24, RefundPercent = 80 },
                new RefundPolicyTier { MinHoursBeforeDeparture = 12, RefundPercent = 50 }
            ],
            BlockBelowHours = 12
        });
        return new RefundPolicyService(options);
    }

    [Theory]
    [InlineData(72, 80, 800)]   // ≥24h → 80%
    [InlineData(24, 80, 800)]   // boundary
    [InlineData(23.5, 50, 500)] // 12–24h → 50%
    [InlineData(12, 50, 500)]   // boundary
    public void Quote_returns_expected_refund(double hoursAhead, int expectedPercent, int expectedAmount)
    {
        var svc = Build();
        var now = new DateTime(2026, 04, 23, 12, 0, 0, DateTimeKind.Utc);
        var departure = now.AddHours(hoursAhead);

        var quote = svc.Quote(totalAmount: 1000m, departureUtc: departure, nowUtc: now);

        quote.Blocked.Should().BeFalse();
        quote.RefundPercent.Should().Be(expectedPercent);
        quote.RefundAmount.Should().Be(expectedAmount);
        quote.HoursUntilDeparture.Should().BeApproximately(hoursAhead, 0.001);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(11.99)]
    public void Quote_blocks_under_window(double hoursAhead)
    {
        var svc = Build();
        var now = new DateTime(2026, 04, 23, 12, 0, 0, DateTimeKind.Utc);
        var quote = svc.Quote(1000m, now.AddHours(hoursAhead), now);

        quote.Blocked.Should().BeTrue();
        quote.RefundPercent.Should().Be(0);
        quote.RefundAmount.Should().Be(0m);
    }

    [Fact]
    public void Quote_after_departure_is_blocked()
    {
        var svc = Build();
        var now = new DateTime(2026, 04, 23, 12, 0, 0, DateTimeKind.Utc);
        var quote = svc.Quote(1000m, now.AddHours(-1), now);
        quote.Blocked.Should().BeTrue();
        quote.HoursUntilDeparture.Should().BeApproximately(-1, 0.001);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Utc,         DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local,       DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Utc,         DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Unspecified)]
    public void Quote_throws_when_either_datetime_is_not_utc(
        DateTimeKind departureKind, DateTimeKind nowKind)
    {
        var svc = Build();
        var dep = new DateTime(2026, 4, 24, 12, 0, 0, departureKind);
        var now = new DateTime(2026, 4, 23, 12, 0, 0, nowKind);

        Action act = () => svc.Quote(1000m, dep, now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Quote_in_gap_between_block_and_lowest_tier_returns_zero_percent_not_blocked()
    {
        var options = Options.Create(new RefundPolicyOptions
        {
            Tiers = [new RefundPolicyTier { MinHoursBeforeDeparture = 24, RefundPercent = 80 }],
            BlockBelowHours = 6
        });
        var svc = new RefundPolicyService(options);
        var now = new DateTime(2026, 04, 23, 12, 0, 0, DateTimeKind.Utc);

        var quote = svc.Quote(1000m, now.AddHours(12), now);

        quote.Blocked.Should().BeFalse();
        quote.RefundPercent.Should().Be(0);
        quote.RefundAmount.Should().Be(0m);
        quote.HoursUntilDeparture.Should().BeApproximately(12, 0.001);
    }
}
