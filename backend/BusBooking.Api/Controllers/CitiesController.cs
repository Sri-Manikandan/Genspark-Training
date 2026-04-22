using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/cities")]
[AllowAnonymous]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cities;

    public CitiesController(ICityService cities)
    {
        _cities = cities;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CityDto>>> Search(
        [FromQuery] string q = "",
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var results = await _cities.SearchActiveAsync(q, limit, ct);
        return Ok(results);
    }
}
