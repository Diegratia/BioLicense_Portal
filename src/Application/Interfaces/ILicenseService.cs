using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BioLicense_Portal.Application.Interfaces
{
    public interface ILicenseService
    {
        Task<LicenseRequestResponseDto> CreateRequestAsync(Guid distributorId, CreateLicenseRequestDto request);
        Task<IEnumerable<LicenseRequestResponseDto>> GetMyRequestsAsync(Guid distributorId);
        Task<IEnumerable<LicenseRequestResponseDto>> GetPendingRequestsAsync();
        Task<bool> ApproveRequestAsync(Guid requestId, Guid engineerId);
        Task<bool> RejectRequestAsync(Guid requestId, Guid engineerId, string reason);
    }

    public record CreateLicenseRequestDto(
        Guid ApplicationId,
        string CustomerName,
        string CustomerEmail,
        string DeviceId,
        BioLicense_Portal.Domain.Enums.LicenseType LicenseType,
        BioLicense_Portal.Domain.Enums.LicenseTier LicenseTier,
        DateTime? ExpiryDate,
        List<string> SelectedFeatures,
        Dictionary<string, object> Parameters,
        string? Notes
    );

    public record LicenseRequestResponseDto(
        Guid Id,
        string ApplicationName,
        string DistributorName,
        string CustomerName,
        string DeviceId,
        int Status,
        string StatusText,
        string Features,
        string? LicenseParameters,
        DateTime CreatedAt,
        DateTime? ExpiryDate,
        string? Notes,
        string? RejectionReason
    );
}
