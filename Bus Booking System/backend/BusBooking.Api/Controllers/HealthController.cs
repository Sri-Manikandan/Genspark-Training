using BusBooking.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponseDto> Get()
    {
        return Ok(new HealthResponseDto(
            Status: "ok",
            Service: "bus-booking-api",
            Version: "0.1.0",
            TimestampUtc: DateTime.UtcNow));
    }
}
