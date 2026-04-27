using BusBooking.Api.Dtos;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/revenue")]
[Authorize(Roles = "admin")]
public class AdminRevenueController : ControllerBase
{
    private readonly IAdminRevenueService _service;

    public AdminRevenueController(IAdminRevenueService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<AdminRevenueResponseDto>> Get(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? today.AddMonths(-1);
        var t = to ?? today;
        return Ok(await _service.GetAsync(f, t, ct));
    }
}
