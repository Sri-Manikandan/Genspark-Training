using System.Security.Claims;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/platform-fee")]
[Authorize(Roles = "admin")]
public class AdminPlatformFeeController : ControllerBase
{
    private readonly IPlatformFeeService _fees;

    public AdminPlatformFeeController(IPlatformFeeService fees)
    {
        _fees = fees;
    }

    [HttpGet]
    public async Task<ActionResult<PlatformFeeDto>> GetActive(CancellationToken ct)
        => Ok(await _fees.GetActiveAsync(ct));

    [HttpPut]
    public async Task<ActionResult<PlatformFeeDto>> Update(
        [FromBody] UpdatePlatformFeeRequest body,
        [FromServices] IValidator<UpdatePlatformFeeRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedException();
        var adminId = Guid.Parse(sub);
        var updated = await _fees.UpdateAsync(adminId, body, ct);
        return Ok(updated);
    }
}
