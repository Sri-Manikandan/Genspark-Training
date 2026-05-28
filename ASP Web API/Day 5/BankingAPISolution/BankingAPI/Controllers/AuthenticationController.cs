using BankingAPI.Interfaces;
using BankingAPI.Misc;
using BankingAPI.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Authentication;
using BankingAPI.Filters;

namespace BankingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("Register")]
        [CustomExceptionFilter]
        public async Task<ActionResult<RegisterUserResponse>> RegisterUser(RegisterUserRequest request)
        {
            var result = await _authenticationService.Register(request);
            return Ok(result);
        }

        [HttpPost("Login")]
        [CustomExceptionFilter]
        public async Task<ActionResult<LoginResponse>> CustomerLogin(LoginRequest request)
        {
            var result = await _authenticationService.Login(request);
            return Ok(result);
        }
    }
}
