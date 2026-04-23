using BusBooking.Api.Services;
using FluentAssertions;

namespace BusBooking.Api.Tests.Unit;

public class ScheduleDayOfWeekTests
{
    // 2026-04-27 is a Monday
    [Theory]
    [InlineData("2026-04-27", 1,   true)]   // Monday, Mon flag set
    [InlineData("2026-04-27", 2,   false)]  // Monday, only Tue flag set
    [InlineData("2026-04-27", 127, true)]   // Monday, all days set
    [InlineData("2026-04-26", 64,  true)]   // Sunday = bit6 = 64
    [InlineData("2026-04-25", 32,  true)]   // Saturday = bit5 = 32
    [InlineData("2026-04-24", 16,  true)]   // Friday = bit4 = 16
    [InlineData("2026-04-23", 8,   true)]   // Thursday = bit3 = 8
    [InlineData("2026-04-22", 4,   true)]   // Wednesday = bit2 = 4
    [InlineData("2026-04-28", 2,   true)]   // Tuesday = bit1 = 2
    public void GetDayBit_returns_correct_flag(string dateStr, int mask, bool expected)
    {
        var date = DateOnly.Parse(dateStr);
        var bit = ScheduleService.GetDayBit(date.DayOfWeek);
        ((mask & bit) != 0).Should().Be(expected);
    }

    [Fact]
    public void ScheduleRunsOnDate_true_when_day_and_range_match()
    {
        // Monday 2026-04-27, mask Mon=1, valid range includes that date
        TripService.ScheduleRunsOnDate(1, DateOnly.Parse("2026-04-27"),
            DateOnly.Parse("2026-04-01"), DateOnly.Parse("2026-04-30"))
            .Should().BeTrue();
    }

    [Fact]
    public void ScheduleRunsOnDate_false_when_day_outside_range()
    {
        TripService.ScheduleRunsOnDate(1, DateOnly.Parse("2026-04-27"),
            DateOnly.Parse("2026-05-01"), DateOnly.Parse("2026-05-31"))
            .Should().BeFalse();
    }

    [Fact]
    public void ScheduleRunsOnDate_false_when_day_bit_not_set()
    {
        // Monday, mask=2 (Tuesday only)
        TripService.ScheduleRunsOnDate(2, DateOnly.Parse("2026-04-27"),
            DateOnly.Parse("2026-04-01"), DateOnly.Parse("2026-04-30"))
            .Should().BeFalse();
    }
}
