using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/cities")]
[Authorize(Roles = "admin")]
public class AdminCitiesController : ControllerBase
{
    private readonly ICityService _cities;

    public AdminCitiesController(ICityService cities)
    {
        _cities = cities;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CityDto>>> List(CancellationToken ct)
        => Ok(await _cities.ListAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CityDto>> Get(Guid id, CancellationToken ct)
        => Ok(await _cities.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CityDto>> Create(
        [FromBody] CreateCityRequest body,
        [FromServices] IValidator<CreateCityRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var city = await _cities.CreateAsync(body, ct);
        return StatusCode(StatusCodes.Status201Created, city);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CityDto>> Update(
        Guid id,
        [FromBody] UpdateCityRequest body,
        [FromServices] IValidator<UpdateCityRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var city = await _cities.UpdateAsync(id, body, ct);
        return Ok(city);
    }
}
