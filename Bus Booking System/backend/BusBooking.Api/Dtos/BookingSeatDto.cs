namespace BusBooking.Api.Dtos;

public record BookingSeatDto(
    string SeatNumber,
    string PassengerName,
    int PassengerAge,
    string PassengerGender);

