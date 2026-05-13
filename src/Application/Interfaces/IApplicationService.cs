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
        
        // Tier Management
        Task AddTierAsync(Guid appId, CreateTierRequestDto request);
        Task<bool> UpdateTierAsync(Guid tierId, UpdateTierRequestDto request);
        Task<bool> DeleteTierAsync(Guid tierId);
    }

    public record CreateAppRequestDto(string Name, string Slug, ApplicationType Type, string? Description);
    public record UpdateAppRequestDto(string Name, string? Description, int Status);
    public record CreateFeatureRequestDto(string FeatureKey, string DisplayName, string? Description, string Category = "core");
    public record UpdateFeatureRequestDto(string DisplayName, string? Description, string Category, bool IsActive);
    
    public record CreateTierRequestDto(LicenseTier Tier, string? Description, List<Guid>? FeatureIds, Dictionary<string, object>? Parameters);
    public record UpdateTierRequestDto(LicenseTier Tier, string? Description, List<Guid>? FeatureIds, Dictionary<string, object>? Parameters);
    
    public record AppResponseDto(Guid Id, string Name, string Slug, ApplicationType Type, string? Description, string? PublicKey, int Status, List<TierResponseDto> Tiers, List<FeatureResponseDto> Features);
    public record FeatureResponseDto(Guid Id, string FeatureKey, string DisplayName, string Category, bool IsActive);
    public record TierResponseDto(Guid Id, LicenseTier Tier, string? Description, string? Features, string? Parameters);
}
