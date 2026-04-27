using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Models;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/operator/bookings")]
[Authorize(Roles = Roles.Operator)]
public class OperatorBookingsController : ControllerBase
{
    private readonly IOperatorBookingService _service;
    private readonly ICurrentUserAccessor _me;

    public OperatorBookingsController(IOperatorBookingService service, ICurrentUserAccessor me)
    {
        _service = service;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<OperatorBookingListResponseDto>> List(
        [FromQuery] Guid? busId,
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _service.ListBookingsAsync(_me.UserId, busId, date, page, pageSize, ct));
}
