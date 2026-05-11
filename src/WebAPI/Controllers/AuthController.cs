using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Application.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BioLicense_Portal.WebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);
            if (response == null)
            {
                return Unauthorized(ApiResponse.Unauthorized("Invalid username or password"));
            }

            return Ok(ApiResponse.Success("Login successful", response));
        }

        [HttpPost("seed-owner")]
        public async Task<IActionResult> SeedOwner()
        {
            await _authService.SeedOwnerAsync();
            return Ok(ApiResponse.Success("Owner seeded successfully"));
        }
    }
}
