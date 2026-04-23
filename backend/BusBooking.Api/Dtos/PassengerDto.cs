namespace BusBooking.Api.Dtos;

public record PassengerDto(
    string SeatNumber,
    string PassengerName,
    int PassengerAge,
    string PassengerGender);

