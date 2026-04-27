using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/operator/buses")]
[Authorize(Roles = "operator")]
public class OperatorBusesController : ControllerBase
{
    private readonly IBusService _buses;
    private readonly ICurrentUserAccessor _me;

    public OperatorBusesController(IBusService buses, ICurrentUserAccessor me)
    {
        _buses = buses;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusDto>>> List(CancellationToken ct)
        => Ok(await _buses.ListForOperatorAsync(_me.UserId, ct));

    [HttpPost]
    public async Task<ActionResult<BusDto>> Create(
        [FromBody] CreateBusRequest body,
        [FromServices] IValidator<CreateBusRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var dto = await _buses.CreateAsync(_me.UserId, body, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<BusDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateBusStatusRequest body,
        [FromServices] IValidator<UpdateBusStatusRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        return Ok(await _buses.UpdateOperationalStatusAsync(_me.UserId, id, body.OperationalStatus, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<BusDto>> Retire(Guid id, CancellationToken ct)
        => Ok(await _buses.RetireAsync(_me.UserId, id, ct));
}
