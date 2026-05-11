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
                Features = string.Join(",", request.SelectedFeatures),
                LicenseParameters = JsonSerializer.Serialize(request.Parameters),
                RequestStatus = BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Pending,
                Notes = request.Notes,
                RequestedAt = DateTime.UtcNow
            };

            await _licenseRepository.AddRequestAsync(licenseRequest);
            
            // Reload to get includes
            var savedRequest = await _licenseRepository.GetRequestByIdAsync(licenseRequest.Id);
            return MapToDto(savedRequest!);
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
