using Microsoft.AspNetCore.Mvc;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Auth;

namespace TiketApp.Api.Controllers
{
  
        [Route("api/[controller]")]
        [ApiController]
        public class AuthController : ControllerBase
        {
            private readonly IAuthService _authService;

            public AuthController(IAuthService authService)
            {
                _authService = authService;
            }

            [HttpPost("login")]
            public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
            {
                var result = await _authService.LoginAsync(loginRequest);

                if (result == null)
                    return Unauthorized(new { message = "Invalid credentials" });

                return Ok(result);
            }
        }
    }
