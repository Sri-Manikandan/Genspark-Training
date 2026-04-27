using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/operator/revenue")]
[Authorize(Roles = Roles.Operator)]
public class OperatorRevenueController : ControllerBase
{
    private readonly IOperatorBookingService _service;
    private readonly ICurrentUserAccessor _me;

    public OperatorRevenueController(IOperatorBookingService service, ICurrentUserAccessor me)
    {
        _service = service;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<OperatorRevenueResponseDto>> Get(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? today.AddMonths(-1);
        var t = to ?? today;
        return Ok(await _service.GetRevenueAsync(_me.UserId, f, t, ct));
    }
}
