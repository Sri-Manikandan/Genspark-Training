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
        [FromQuery] DateOnly? date,
        [FromQuery] string? busType,
        CancellationToken ct)
    {
        // Compiler resolves each call to the correct SearchAsync overload at build time —
        // this is static (compile-time) polymorphism / method overloading.
        if (date is null)
            return Ok(await _trips.SearchAsync(src, dst, ct));

        if (busType is not null)
            return Ok(await _trips.SearchAsync(src, dst, date.Value, busType, ct));

        return Ok(await _trips.SearchAsync(src, dst, date.Value, ct));
    }
}
