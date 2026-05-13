using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Application.Extensions;
using BioLicense_Portal.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Threading.Tasks;

namespace BioLicense_Portal.WebAPI.Controllers
{
    [ApiController]
    [Route("api/licenses")]
    [Authorize]
    public class LicenseRecordController : ControllerBase
    {
        private readonly ILicenseService _licenseService;

        public LicenseRecordController(ILicenseService licenseService)
        {
            _licenseService = licenseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _licenseService.GetAllLicensesAsync();
            return Ok(ApiResponse.Success(Messages.General.Success, result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _licenseService.GetLicenseByIdAsync(id);
            if (result == null) return NotFound(ApiResponse.NotFound(Messages.General.NotFound));
            
            return Ok(ApiResponse.Success(Messages.General.Success, result));
        }

        [HttpPost("direct")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> CreateDirect([FromBody] CreateLicenseRequestDto request)
        {
            var userId = GetUserId();
            var result = await _licenseService.CreateLicenseDirectAsync(userId, request);
            return Ok(ApiResponse.Success(Messages.License.Generated, result));
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(Guid id)
        {
            var content = await _licenseService.GetLicenseContentAsync(id);
            if (string.IsNullOrEmpty(content)) return NotFound(ApiResponse.NotFound("License file not found"));

            var license = await _licenseService.GetLicenseByIdAsync(id);
            var filename = $"{license?.ApplicationName ?? "License"}_{license?.CustomerName ?? "Client"}.lic";
            
            return File(Encoding.UTF8.GetBytes(content), "application/octet-stream", filename);
        }

        [HttpPost("{id}/revoke")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokeRequestModel model)
        {
            var success = await _licenseService.RevokeLicenseAsync(id, model.Reason);
            if (!success) return BadRequest(ApiResponse.BadRequest("License not found or already revoked"));

            return Ok(ApiResponse.Success("License revoked successfully"));
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) throw new UnauthorizedAccessException();
            return Guid.Parse(userIdClaim);
        }
    }

    public record RevokeRequestModel(string Reason);
}
