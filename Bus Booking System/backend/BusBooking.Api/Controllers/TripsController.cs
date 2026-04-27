using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/trips")]
[AllowAnonymous]
public class TripsController : ControllerBase
{
    private readonly ITripService _trips;
    public TripsController(ITripService trips) => _trips = trips;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripDetailDto>> GetDetail(Guid id, CancellationToken ct)
        => Ok(await _trips.GetDetailAsync(id, ct));

    [HttpGet("{id:guid}/seats")]
    public async Task<ActionResult<SeatLayoutDto>> GetSeats(Guid id, CancellationToken ct)
        => Ok(await _trips.GetSeatLayoutAsync(id, ct));
}
