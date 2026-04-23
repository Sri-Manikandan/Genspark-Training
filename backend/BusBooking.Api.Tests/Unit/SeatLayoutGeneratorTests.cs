using BusBooking.Api.Services;
using FluentAssertions;

namespace BusBooking.Api.Tests.Unit;

public class SeatLayoutGeneratorTests
{
    [Fact]
    public void Three_by_four_produces_12_seats_with_expected_labels()
    {
        var busId = Guid.NewGuid();
        var seats = SeatLayoutGenerator.Generate(busId, rows: 3, columns: 4);

        seats.Should().HaveCount(12);
        seats.Select(s => s.SeatNumber).Should().ContainInOrder(
            "A1", "A2", "A3", "A4",
            "B1", "B2", "B3", "B4",
            "C1", "C2", "C3", "C4");
        seats.Should().OnlyContain(s => s.BusId == busId);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 0)]
    [InlineData(27, 2)]
    [InlineData(2, 13)]
    public void Out_of_range_dimensions_throw(int rows, int columns)
    {
        Action act = () => SeatLayoutGenerator.Generate(Guid.NewGuid(), rows, columns);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Seat_categories_default_to_regular()
    {
        var seats = SeatLayoutGenerator.Generate(Guid.NewGuid(), 1, 2);
        seats.Should().OnlyContain(s => s.SeatCategory == "regular");
    }
}
