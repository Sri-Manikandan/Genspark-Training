using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[AllowAnonymous]
public class SearchController : ControllerBase
{
    private readonly ITripService _trips;
    public SearchController(ITripService trips) => _trips = trips;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SearchResultDto>>> Search(
        [FromQuery] Guid src,
        [FromQuery] Guid dst,
        [FromQuery] DateOnly date,
        CancellationToken ct)
        => Ok(await _trips.SearchAsync(src, dst, date, ct));
}
