using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/operator-requests")]
[Authorize(Roles = "admin")]
public class AdminOperatorRequestsController : ControllerBase
{
    private readonly IOperatorRequestService _requests;
    private readonly ICurrentUserAccessor _me;

    public AdminOperatorRequestsController(IOperatorRequestService requests, ICurrentUserAccessor me)
    {
        _requests = requests;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OperatorRequestDto>>> List(
        [FromQuery] string? status, CancellationToken ct)
        => Ok(await _requests.ListAsync(status, ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<OperatorRequestDto>> Approve(Guid id, CancellationToken ct)
        => Ok(await _requests.ApproveAsync(_me.UserId, id, ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<OperatorRequestDto>> Reject(
        Guid id,
        [FromBody] RejectOperatorRequest body,
        [FromServices] IValidator<RejectOperatorRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(body, ct);
        return Ok(await _requests.RejectAsync(_me.UserId, id, body.Reason, ct));
    }
}
