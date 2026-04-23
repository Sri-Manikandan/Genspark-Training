namespace BusBooking.Api.Models;

public class SeatDefinition
{
    public Guid Id { get; set; }
    public Guid BusId { get; set; }
    public required string SeatNumber { get; set; }
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public required string SeatCategory { get; set; } = Models.SeatCategory.Regular;

    public Bus? Bus { get; set; }
}
