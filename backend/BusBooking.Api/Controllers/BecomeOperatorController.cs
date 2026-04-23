using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/me/become-operator")]
[Authorize(Roles = "customer")]
public class BecomeOperatorController : ControllerBase
{
    private readonly IOperatorRequestService _requests;
    private readonly ICurrentUserAccessor _me;

    public BecomeOperatorController(IOperatorRequestService requests, ICurrentUserAccessor me)
    {
        _requests = requests;
        _me = me;
    }

    [HttpPost]
    public async Task<ActionResult<OperatorRequestDto>> Submit(
        [FromBody] BecomeOperatorRequest body,
        [FromServices] IValidator<BecomeOperatorRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        var dto = await _requests.SubmitAsync(_me.UserId, body, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }
}
