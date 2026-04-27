using BusBooking.Api.Models;

namespace BusBooking.Api.Services;

public static class SeatLayoutGenerator
{
    public const int MaxRows = 26;     // A..Z
    public const int MaxColumns = 12;

    public static IReadOnlyList<SeatDefinition> Generate(Guid busId, int rows, int columns)
    {
        if (rows < 1 || rows > MaxRows)
            throw new ArgumentOutOfRangeException(nameof(rows),
                $"Rows must be between 1 and {MaxRows}");
        if (columns < 1 || columns > MaxColumns)
            throw new ArgumentOutOfRangeException(nameof(columns),
                $"Columns must be between 1 and {MaxColumns}");

        var seats = new List<SeatDefinition>(rows * columns);
        for (var r = 0; r < rows; r++)
        {
            var rowLetter = (char)('A' + r);
            for (var c = 0; c < columns; c++)
            {
                seats.Add(new SeatDefinition
                {
                    Id = Guid.NewGuid(),
                    BusId = busId,
                    SeatNumber = $"{rowLetter}{c + 1}",
                    RowIndex = r,
                    ColumnIndex = c,
                    SeatCategory = SeatCategory.Regular
                });
            }
        }
        return seats;
    }
}
