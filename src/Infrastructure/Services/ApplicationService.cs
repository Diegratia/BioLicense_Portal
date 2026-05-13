using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Domain.Entities;
using BioLicense_Portal.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AppEntity = BioLicense_Portal.Domain.Entities.Application;

namespace BioLicense_Portal.Infrastructure.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly ApplicationRepository _repository;
        private readonly IKeyGeneratorService _keyGenerator;

        public ApplicationService(ApplicationRepository repository, IKeyGeneratorService keyGenerator)
        {
            _repository = repository;
            _keyGenerator = keyGenerator;
        }

        public async Task<IEnumerable<AppResponseDto>> GetAllAsync(string? search = null, int? status = null)
        {
            return await _repository.GetListAsync(search, status);
        }

        public async Task<AppResponseDto?> GetByIdAsync(Guid id)
        {
            return await _repository.GetProjectedByIdAsync(id);
        }

        public async Task<AppResponseDto> CreateAsync(CreateAppRequestDto request)
        {
            var existing = await _repository.GetBySlugAsync(request.Slug);
            if (existing != null) throw new InvalidOperationException("Application with this slug already exists.");

            var (pub, encryptedPriv) = _keyGenerator.GenerateKeyPair();

            var app = new AppEntity
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Slug,
                ApplicationType = request.Type,
                Description = request.Description,
                PublicKey = pub,
                PrivateKeyEncrypted = encryptedPriv,
                Status = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(app);
            
            return new AppResponseDto(
                app.Id, app.Name, app.Slug, app.ApplicationType, 
                app.Description, app.PublicKey, app.Status, null, new List<FeatureResponseDto>());
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateAppRequestDto request)
        {
            var app = await _repository.GetByIdAsync(id);
            if (app == null) return false;

            app.Name = request.Name;
            app.Description = request.Description;
            app.Status = request.Status;
            app.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(app);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var app = await _repository.GetByIdAsync(id);
            if (app == null) return false;

            await _repository.DeleteAsync(app);
            return true;
        }

        public async Task AddFeatureAsync(Guid appId, CreateFeatureRequestDto request)
        {
            var app = await _repository.GetByIdAsync(appId);
            if (app == null) throw new KeyNotFoundException("Application not found.");

            var feature = new ApplicationFeature
            {
                Id = Guid.NewGuid(),
                ApplicationId = appId,
                FeatureKey = request.FeatureKey,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Category = request.Category,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddFeatureAsync(feature);
        }

        public async Task<bool> UpdateFeatureAsync(Guid featureId, UpdateFeatureRequestDto request)
        {
            var feature = await _repository.GetFeatureByIdAsync(featureId);
            if (feature == null) return false;

            feature.DisplayName = request.DisplayName;
            feature.Description = request.Description;
            feature.Category = request.Category;
            feature.IsActive = request.IsActive;
            feature.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateFeatureAsync(feature);
            return true;
        }

        public async Task<bool> DeleteFeatureAsync(Guid featureId)
        {
            var feature = await _repository.GetFeatureByIdAsync(featureId);
            if (feature == null) return false;

            await _repository.DeleteFeatureAsync(feature);
            return true;
        }

        public async Task AddTierAsync(Guid appId, CreateTierRequestDto request)
        {
            var app = await _repository.GetByIdAsync(appId);
            if (app == null) throw new KeyNotFoundException("Application not found.");

            // Avoid duplicates
            if (app.Tiers.Any(t => t.Tier == request.Tier))
                throw new InvalidOperationException($"Tier {request.Tier} already exists for this application.");

            var tier = new ApplicationTier
            {
                Id = Guid.NewGuid(),
                ApplicationId = appId,
                Tier = request.Tier,
                Description = request.Description,
                Parameters = request.Parameters != null ? JsonSerializer.Serialize(request.Parameters) : null,
                CreatedAt = DateTime.UtcNow
            };

            if (request.FeatureIds != null)
            {
                foreach (var fId in request.FeatureIds)
                {
                    if (app.Features.Any(f => f.Id == fId))
                    {
                        tier.TierFeatures.Add(new ApplicationTierFeature { TierId = tier.Id, FeatureId = fId });
                    }
                }
            }

            await _repository.AddTierAsync(tier);
        }

        public async Task<bool> UpdateTierAsync(Guid tierId, UpdateTierRequestDto request)
        {
            var tier = await _repository.GetTierByIdAsync(tierId);
            if (tier == null) return false;

            // Load app to validate features
            var app = await _repository.GetByIdAsync(tier.ApplicationId);

            tier.Tier = request.Tier;
            tier.Description = request.Description;
            tier.Parameters = request.Parameters != null ? JsonSerializer.Serialize(request.Parameters) : null;
            tier.UpdatedAt = DateTime.UtcNow;

            // Update Features (Many-to-Many)
            tier.TierFeatures.Clear();
            if (request.FeatureIds != null)
            {
                foreach (var fId in request.FeatureIds)
                {
                    if (app != null && app.Features.Any(f => f.Id == fId))
                    {
                        tier.TierFeatures.Add(new ApplicationTierFeature { TierId = tier.Id, FeatureId = fId });
                    }
                }
            }

            await _repository.UpdateTierAsync(tier);
            return true;
        }

        public async Task<bool> DeleteTierAsync(Guid tierId)
        {
            var tier = await _repository.GetTierByIdAsync(tierId);
            if (tier == null) return false;

            await _repository.DeleteTierAsync(tier);
            return true;
        }
    }
}
