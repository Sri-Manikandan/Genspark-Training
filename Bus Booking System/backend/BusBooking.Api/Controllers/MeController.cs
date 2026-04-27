using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly AppDbContext _db;

    public MeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<UserDto>> Get(CancellationToken ct)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedException("UNAUTHORIZED", "Missing subject claim");

        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedException("UNAUTHORIZED", "Invalid subject claim");

        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException("UNAUTHORIZED", "User no longer exists");

        var dto = new UserDto(
            user.Id, user.Name, user.Email, user.Phone,
            user.Roles.Select(r => r.Role).ToArray());
        return Ok(dto);
    }
}
