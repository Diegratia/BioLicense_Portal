using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Domain.Entities;
using BioLicense_Portal.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var (pub, priv) = _keyGenerator.GenerateKeyPair();
            var encryptedPriv = _keyGenerator.EncryptPrivateKey(priv, request.KeyPassphrase);

            var app = new AppEntity
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Slug,
                ApplicationType = request.Type,
                Description = request.Description,
                PublicKey = pub,
                PrivateKeyEncrypted = encryptedPriv,
                KeyPassphrase = request.KeyPassphrase,
                Status = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(app);
            
            // Manual mapping for single response
            return new AppResponseDto(
                app.Id, app.Name, app.Slug, app.ApplicationType, 
                app.Description, app.PublicKey, app.Status, new List<FeatureResponseDto>());
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
    }
}
