using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Application.Extensions;
using BioLicense_Portal.Application.Common.Constants;
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
                return Unauthorized(ApiResponse.Unauthorized(Messages.Auth.InvalidCredentials));
            }

            return Ok(ApiResponse.Success(Messages.Auth.LoginSuccess, response));
        }

        [HttpPost("seed-owner")]
        public async Task<IActionResult> SeedOwner()
        {
            await _authService.SeedOwnerAsync();
            return Ok(ApiResponse.Success(Messages.Auth.SeedSuccess));
        }

        [HttpPost("seed-distributor")]
        public async Task<IActionResult> SeedDistributor()
        {
            await _authService.SeedDistributorAsync();
            return Ok(ApiResponse.Success(Messages.Auth.SeedSuccess));
        }
    }
}
