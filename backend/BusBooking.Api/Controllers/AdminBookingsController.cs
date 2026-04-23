using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/bookings")]
[Authorize(Roles = "admin")]
public class AdminBookingsController : ControllerBase
{
    private readonly IAdminBookingService _service;

    public AdminBookingsController(IAdminBookingService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<AdminBookingListResponseDto>> List(
        [FromQuery] Guid? operatorUserId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _service.ListAsync(operatorUserId, status, date, page, pageSize, ct));
}
