using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Domain.Entities;
using BioLicense_Portal.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BioLicense_Portal.Infrastructure.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly LicenseRepository _licenseRepository;
        private readonly ApplicationRepository _appRepository;

        public LicenseService(LicenseRepository licenseRepository, ApplicationRepository appRepository)
        {
            _licenseRepository = licenseRepository;
            _appRepository = appRepository;
        }

        public async Task<LicenseRequestResponseDto> CreateRequestAsync(Guid distributorId, CreateLicenseRequestDto request)
        {
            var app = await _appRepository.GetByIdAsync(request.ApplicationId);
            if (app == null) throw new KeyNotFoundException("Application not found.");

            // Standardize Features and Parameters based on Tier if not Custom
            var finalFeatures = request.SelectedFeatures;
            var finalParameters = request.Parameters;

            if (request.LicenseTier != BioLicense_Portal.Domain.Enums.LicenseTier.Custom && !string.IsNullOrEmpty(app.TierConfigs))
            {
                try
                {
                    var configs = JsonSerializer.Deserialize<Dictionary<string, TierConfigItem>>(app.TierConfigs);
                    if (configs != null && configs.TryGetValue(request.LicenseTier.ToString(), out var config))
                    {
                        finalFeatures = config.Features ?? finalFeatures;
                        finalParameters = config.Parameters ?? finalParameters;
                    }
                }
                catch (JsonException)
                {
                    // Fallback to request values if config is invalid or log error
                }
            }

            var licenseRequest = new LicenseRequest
            {
                Id = Guid.NewGuid(),
                ApplicationId = request.ApplicationId,
                RequesterUserId = distributorId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                MachineId = request.DeviceId,
                LicenseType = request.LicenseType,
                LicenseTier = request.LicenseTier,
                ExpiryDate = request.ExpiryDate,
                Features = string.Join(",", finalFeatures),
                LicenseParameters = JsonSerializer.Serialize(finalParameters),
                RequestStatus = BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Pending,
                Notes = request.Notes,
                RequestedAt = DateTime.UtcNow
            };

            await _licenseRepository.AddRequestAsync(licenseRequest);
            
            // Reload to get includes
            var savedRequest = await _licenseRepository.GetRequestByIdAsync(licenseRequest.Id);
            return MapToDto(savedRequest!);
        }

        private class TierConfigItem
        {
            public List<string>? Features { get; set; }
            public Dictionary<string, object>? Parameters { get; set; }
        }

        public async Task<IEnumerable<LicenseRequestResponseDto>> GetMyRequestsAsync(Guid distributorId)
        {
            var requests = await _licenseRepository.GetRequestsByDistributorAsync(distributorId);
            return requests.Select(MapToDto);
        }

        public async Task<IEnumerable<LicenseRequestResponseDto>> GetPendingRequestsAsync()
        {
            var requests = await _licenseRepository.GetPendingRequestsAsync();
            return requests.Select(MapToDto);
        }

        public async Task<bool> ApproveRequestAsync(Guid requestId, Guid engineerId)
        {
            var request = await _licenseRepository.GetRequestByIdAsync(requestId);
            if (request == null || request.RequestStatus != BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Pending) return false;

            request.RequestStatus = BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Approved;
            request.ApproverUserId = engineerId;
            request.ProcessedAt = DateTime.UtcNow;

            await _licenseRepository.UpdateRequestAsync(request);
            return true;
        }

        public async Task<bool> RejectRequestAsync(Guid requestId, Guid engineerId, string reason)
        {
            var request = await _licenseRepository.GetRequestByIdAsync(requestId);
            if (request == null || request.RequestStatus != BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Pending) return false;

            request.RequestStatus = BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Rejected;
            request.RejectionReason = reason;
            request.ApproverUserId = engineerId;
            request.ProcessedAt = DateTime.UtcNow;

            await _licenseRepository.UpdateRequestAsync(request);
            return true;
        }

        private LicenseRequestResponseDto MapToDto(LicenseRequest request)
        {
            return new LicenseRequestResponseDto(
                request.Id,
                request.Application?.Name ?? "Unknown",
                request.Requester?.Username ?? "Unknown",
                request.CustomerName,
                request.MachineId,
                (int)request.RequestStatus,
                request.RequestStatus.ToString(),
                request.Features ?? "",
                request.LicenseParameters,
                request.RequestedAt,
                request.ExpiryDate,
                request.Notes,
                request.RejectionReason
            );
        }
    }
}
