using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class SeatLocksController : ControllerBase
{
    private readonly ISeatLockService _locks;
    private readonly IValidator<LockSeatsRequest> _validator;
    private readonly ICurrentUserAccessor _currentUser;

    public SeatLocksController(
        ISeatLockService locks,
        IValidator<LockSeatsRequest> validator,
        ICurrentUserAccessor currentUser)
    {
        _locks = locks;
        _validator = validator;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpPost("trips/{tripId:guid}/seat-locks")]
    public async Task<IActionResult> Lock(Guid tripId, [FromBody] LockSeatsRequest req, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(req, ct);
        Guid? userId = _currentUser.TryGetUserId(out var id) ? id : null;
        var result = await _locks.LockAsync(tripId, userId, req, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpDelete("seat-locks/{lockId:guid}")]
    public async Task<IActionResult> Release(Guid lockId, [FromQuery] Guid sessionId, CancellationToken ct)
    {
        Guid? userId = _currentUser.TryGetUserId(out var id) ? id : null;
        await _locks.ReleaseAsync(lockId, sessionId, userId, ct);
        return NoContent();
    }
}

