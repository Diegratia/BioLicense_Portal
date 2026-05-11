using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Application.Extensions;
using BioLicense_Portal.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BioLicense_Portal.WebAPI.Controllers
{
    [ApiController]
    [Route("api/application")]
    [Authorize(Roles = "Owner")]
    public class ApplicationController : ControllerBase
    {
        private readonly IApplicationService _appService;

        public ApplicationController(IApplicationService appService)
        {
            _appService = appService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? status)
        {
            var result = await _appService.GetAllAsync(search, status);
            return Ok(ApiResponse.Success(Messages.General.Success, result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _appService.GetByIdAsync(id);
            if (result == null) return NotFound(ApiResponse.NotFound(Messages.General.NotFound));
            
            return Ok(ApiResponse.Success(Messages.General.Success, result));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppRequestDto request)
        {
            var result = await _appService.CreateAsync(request);
            return Ok(ApiResponse.Success(Messages.General.Success, result));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAppRequestDto request)
        {
            var success = await _appService.UpdateAsync(id, request);
            if (!success) return NotFound(ApiResponse.NotFound(Messages.General.NotFound));
            
            return Ok(ApiResponse.Success(Messages.General.Success));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _appService.DeleteAsync(id);
            if (!success) return NotFound(ApiResponse.NotFound(Messages.General.NotFound));
            
            return Ok(ApiResponse.Success(Messages.General.Success));
        }

        [HttpPost("{id}/features")]
        public async Task<IActionResult> AddFeature(Guid id, [FromBody] CreateFeatureRequestDto request)
        {
            await _appService.AddFeatureAsync(id, request);
            return Ok(ApiResponse.Success(Messages.General.Success));
        }

        [HttpPut("features/{featureId}")]
        public async Task<IActionResult> UpdateFeature(Guid featureId, [FromBody] UpdateFeatureRequestDto request)
        {
            var success = await _appService.UpdateFeatureAsync(featureId, request);
            if (!success) return NotFound(ApiResponse.NotFound(Messages.General.NotFound));
            
            return Ok(ApiResponse.Success(Messages.General.Success));
        }

        [HttpDelete("features/{featureId}")]
        public async Task<IActionResult> DeleteFeature(Guid featureId)
        {
            var success = await _appService.DeleteFeatureAsync(featureId);
            if (!success) return NotFound(ApiResponse.NotFound(Messages.General.NotFound));
            
            return Ok(ApiResponse.Success(Messages.General.Success));
        }
    }
}
