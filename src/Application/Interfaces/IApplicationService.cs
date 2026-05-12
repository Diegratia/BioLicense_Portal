using BioLicense_Portal.Domain.Entities;
using BioLicense_Portal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BioLicense_Portal.Application.Interfaces
{
    public interface IApplicationService
    {
        Task<IEnumerable<AppResponseDto>> GetAllAsync(string? search = null, int? status = null);
        Task<AppResponseDto?> GetByIdAsync(Guid id);
        Task<AppResponseDto> CreateAsync(CreateAppRequestDto request);
        Task<bool> UpdateAsync(Guid id, UpdateAppRequestDto request);
        Task<bool> DeleteAsync(Guid id);
        Task AddFeatureAsync(Guid appId, CreateFeatureRequestDto request);
        Task<bool> UpdateFeatureAsync(Guid featureId, UpdateFeatureRequestDto request);
        Task<bool> DeleteFeatureAsync(Guid featureId);
    }

    public record CreateAppRequestDto(string Name, string Slug, ApplicationType Type, string? Description, string KeyPassphrase);
    public record UpdateAppRequestDto(string Name, string? Description, int Status, object? TierConfigs = null);
    public record CreateFeatureRequestDto(string FeatureKey, string DisplayName, string? Description, string Category = "core");
    public record UpdateFeatureRequestDto(string DisplayName, string? Description, string Category, bool IsActive);
    public record AppResponseDto(Guid Id, string Name, string Slug, ApplicationType Type, string? Description, string? PublicKey, int Status, object? TierConfigs, List<FeatureResponseDto> Features);
    public record FeatureResponseDto(Guid Id, string FeatureKey, string DisplayName, string Category, bool IsActive);
}
