using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BioLicense_Portal.Application.Interfaces
{
    public interface ILicenseService
    {
        Task<LicenseRequestResponseDto> CreateRequestAsync(Guid distributorId, CreateLicenseRequestDto request);
        Task<LicenseResponseDto> CreateLicenseDirectAsync(Guid ownerId, CreateLicenseRequestDto request);
        Task<IEnumerable<LicenseRequestResponseDto>> GetMyRequestsAsync(Guid distributorId);
        Task<IEnumerable<LicenseRequestResponseDto>> GetPendingRequestsAsync();
        Task<bool> ApproveRequestAsync(Guid requestId, Guid engineerId);
        Task<bool> RejectRequestAsync(Guid requestId, Guid engineerId, string reason);
        Task<IEnumerable<LicenseResponseDto>> GetAllLicensesAsync();
        Task<LicenseResponseDto?> GetLicenseByIdAsync(Guid licenseId);
        Task<string?> GetLicenseContentAsync(Guid licenseId);
        Task<bool> RevokeLicenseAsync(Guid licenseId, string reason);
    }

    public record CreateLicenseRequestDto(
        Guid ApplicationId,
        string CustomerName,
        string CustomerEmail,
        string DeviceId,
        BioLicense_Portal.Domain.Enums.LicenseType LicenseType,
        BioLicense_Portal.Domain.Enums.LicenseTier LicenseTier,
        DateTime? ExpiryDate,
        List<string>? SelectedFeatures,
        Dictionary<string, object>? Parameters,
        string? Notes,
        Guid? RequesterUserId = null
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

    public record LicenseResponseDto(
        Guid Id,
        string ApplicationName,
        string CustomerName,
        string MachineId,
        BioLicense_Portal.Domain.Enums.LicenseType LicenseType,
        BioLicense_Portal.Domain.Enums.LicenseTier LicenseTier,
        string? Features,
        string? Parameters,
        DateTime IssuedAt,
        DateTime ExpiredAt,
        string Status
    );
}
