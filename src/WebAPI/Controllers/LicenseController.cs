using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Application.Extensions;
using BioLicense_Portal.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BioLicense_Portal.WebAPI.Controllers
{
    [ApiController]
    [Route("api/licenses/requests")]
    [Authorize]
    public class LicenseController : ControllerBase
    {
        private readonly ILicenseService _licenseService;

        public LicenseController(ILicenseService licenseService)
        {
            _licenseService = licenseService;
        }

        [HttpPost]
        [Authorize(Roles = "Distributor")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateLicenseRequestDto request)
        {
            var userId = GetUserId();
            var result = await _licenseService.CreateRequestAsync(userId, request);
            return Ok(ApiResponse.Success(Messages.License.Created, result));
        }

        [HttpGet("my")]
        [Authorize(Roles = "Distributor")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = GetUserId();
            var result = await _licenseService.GetMyRequestsAsync(userId);
            return Ok(ApiResponse.Success(Messages.General.Success, result));
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Engineer,Owner")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var result = await _licenseService.GetPendingRequestsAsync();
            return Ok(ApiResponse.Success(Messages.General.Success, result));
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Engineer,Owner")]
        public async Task<IActionResult> ApproveRequest(Guid id)
        {
            var userId = GetUserId();
            var success = await _licenseService.ApproveRequestAsync(id, userId);
            if (!success) return BadRequest(ApiResponse.BadRequest("Request not found or already processed"));
            
            return Ok(ApiResponse.Success(Messages.License.Approved));
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Engineer,Owner")]
        public async Task<IActionResult> RejectRequest(Guid id, [FromBody] RejectRequestModel model)
        {
            var userId = GetUserId();
            var success = await _licenseService.RejectRequestAsync(id, userId, model.Reason);
            if (!success) return BadRequest(ApiResponse.BadRequest("Request not found or already processed"));
            
            return Ok(ApiResponse.Success(Messages.License.Rejected));
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) throw new UnauthorizedAccessException();
            return Guid.Parse(userIdClaim);
        }
    }

    public record RejectRequestModel(string Reason);
}
