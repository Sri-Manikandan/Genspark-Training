using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/buses")]
[Authorize(Roles = "admin")]
public class AdminBusesController : ControllerBase
{
    private readonly IBusService _buses;
    private readonly ICurrentUserAccessor _me;

    public AdminBusesController(IBusService buses, ICurrentUserAccessor me)
    {
        _buses = buses;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusDto>>> List(
        [FromQuery] string? status, CancellationToken ct)
        => Ok(await _buses.ListByApprovalStatusAsync(status, ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<BusDto>> Approve(Guid id, CancellationToken ct)
        => Ok(await _buses.ApproveAsync(_me.UserId, id, ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<BusDto>> Reject(
        Guid id,
        [FromBody] RejectBusRequest body,
        [FromServices] IValidator<RejectBusRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        return Ok(await _buses.RejectAsync(_me.UserId, id, body.Reason, ct));
    }
}
