using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Authorize(Roles = "operator")]
public class OperatorSchedulesController : ControllerBase
{
    private readonly IScheduleService _schedules;
    private readonly ICurrentUserAccessor _me;

    public OperatorSchedulesController(IScheduleService schedules, ICurrentUserAccessor me)
    {
        _schedules = schedules;
        _me = me;
    }

    [HttpGet("api/v1/operator/schedules")]
    public async Task<ActionResult<IReadOnlyList<BusScheduleDto>>> List(
        [FromQuery] Guid? busId, CancellationToken ct)
        => Ok(await _schedules.ListAsync(_me.UserId, busId, ct));

    [HttpPost("api/v1/operator/schedules")]
    public async Task<ActionResult<BusScheduleDto>> Create(
        [FromBody] CreateBusScheduleRequest body,
        [FromServices] IValidator<CreateBusScheduleRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var dto = await _schedules.CreateAsync(_me.UserId, body, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPatch("api/v1/operator/schedules/{id:guid}")]
    public async Task<ActionResult<BusScheduleDto>> Update(
        Guid id,
        [FromBody] UpdateBusScheduleRequest body,
        [FromServices] IValidator<UpdateBusScheduleRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        return Ok(await _schedules.UpdateAsync(_me.UserId, id, body, ct));
    }

    [HttpDelete("api/v1/operator/schedules/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _schedules.DeleteAsync(_me.UserId, id, ct);
        return NoContent();
    }

    [HttpGet("api/v1/operator/routes")]
    public async Task<ActionResult<IReadOnlyList<RouteOptionDto>>> ListRoutes(CancellationToken ct)
        => Ok(await _schedules.ListActiveRoutesAsync(ct));
}
