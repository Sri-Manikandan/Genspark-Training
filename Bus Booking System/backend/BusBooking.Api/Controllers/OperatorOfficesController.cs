using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/operator/offices")]
[Authorize(Roles = "operator")]
public class OperatorOfficesController : ControllerBase
{
    private readonly IOperatorOfficeService _offices;
    private readonly ICurrentUserAccessor _me;

    public OperatorOfficesController(IOperatorOfficeService offices, ICurrentUserAccessor me)
    {
        _offices = offices;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OperatorOfficeDto>>> List(CancellationToken ct)
        => Ok(await _offices.ListAsync(_me.UserId, ct));

    [HttpPost]
    public async Task<ActionResult<OperatorOfficeDto>> Create(
        [FromBody] CreateOperatorOfficeRequest body,
        [FromServices] IValidator<CreateOperatorOfficeRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var dto = await _offices.CreateAsync(_me.UserId, body, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _offices.DeleteAsync(_me.UserId, id, ct);
        return NoContent();
    }
}
