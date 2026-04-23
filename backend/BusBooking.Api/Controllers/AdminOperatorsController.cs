using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/admin/operators")]
[Authorize(Roles = "admin")]
public class AdminOperatorsController : ControllerBase
{
    private readonly IAdminOperatorService _service;
    private readonly ICurrentUserAccessor _me;

    public AdminOperatorsController(IAdminOperatorService service, ICurrentUserAccessor me)
    {
        _service = service;
        _me = me;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminOperatorListItemDto>>> List(
        CancellationToken ct)
        => Ok(await _service.ListAsync(ct));

    [HttpPost("{id:guid}/disable")]
    public async Task<ActionResult<AdminOperatorListItemDto>> Disable(
        Guid id,
        [FromBody] DisableOperatorRequest? body,
        CancellationToken ct)
        => Ok(await _service.DisableAsync(_me.UserId, id, body?.Reason, ct));

    [HttpPost("{id:guid}/enable")]
    public async Task<ActionResult<AdminOperatorListItemDto>> Enable(
        Guid id, CancellationToken ct)
        => Ok(await _service.EnableAsync(_me.UserId, id, ct));
}
