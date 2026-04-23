namespace BusBooking.Api.Dtos;

public record SeatLayoutDto(
    int Rows,
    int Columns,
    IReadOnlyList<SeatStatusDto> Seats
);

public record SeatStatusDto(
    string SeatNumber,
    int RowIndex,
    int ColumnIndex,
    string Status   // "available" | "locked" | "booked"
);
