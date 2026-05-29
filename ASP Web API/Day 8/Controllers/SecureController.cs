using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Day_8.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecureController:ControllerBase{
    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult PublicEndpoint(){
        return Ok(new { message = "Anyone can see this - no token required." });
    }

    [Authorize]
    [HttpGet("private")]
    public IActionResult PrivateEndpoint(){
        var sub = User.FindFirst("sub")?.Value;

        var allClaims = User.Claims.Select(c=> new {c.Type, c.Value});

        return Ok(new{
            message = "You sent a valid Token!",
            subject=sub,
            claims=allClaims
        });
    }
}