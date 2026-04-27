using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/routes")]
[Authorize(Roles = "admin")]
public class AdminRoutesController : ControllerBase
{
    private readonly IRouteService _routes;

    public AdminRoutesController(IRouteService routes)
    {
        _routes = routes;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RouteDto>>> List(CancellationToken ct)
        => Ok(await _routes.ListAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RouteDto>> Get(Guid id, CancellationToken ct)
        => Ok(await _routes.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<RouteDto>> Create(
        [FromBody] CreateRouteRequest body,
        [FromServices] IValidator<CreateRouteRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var route = await _routes.CreateAsync(body, ct);
        return StatusCode(StatusCodes.Status201Created, route);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<RouteDto>> Update(
        Guid id,
        [FromBody] UpdateRouteRequest body,
        [FromServices] IValidator<UpdateRouteRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var route = await _routes.UpdateAsync(id, body, ct);
        return Ok(route);
    }
}
