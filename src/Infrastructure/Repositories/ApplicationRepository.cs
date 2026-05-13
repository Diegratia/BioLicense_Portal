using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Domain.Entities;
using BioLicense_Portal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AppEntity = BioLicense_Portal.Domain.Entities.Application;

namespace BioLicense_Portal.Infrastructure.Repositories
{
    public class ApplicationRepository
    {
        private readonly BioLicenseDbContext _context;

        public ApplicationRepository(BioLicenseDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AppResponseDto>> GetListAsync(string? search, int? status)
        {
            var query = _context.Applications.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Name.Contains(search) || a.Slug.Contains(search));
            }

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            return await ProjectToRead(query).ToListAsync();
        }

        public async Task<AppResponseDto?> GetProjectedByIdAsync(Guid id)
        {
            return await ProjectToRead(_context.Applications.Where(a => a.Id == id))
                .FirstOrDefaultAsync();
        }

        private IQueryable<AppResponseDto> ProjectToRead(IQueryable<AppEntity> query)
        {
            return query.AsNoTracking().Select(a => new AppResponseDto(
                a.Id,
                a.Name,
                a.Slug,
                a.ApplicationType,
                a.Description,
                a.PublicKey,
                a.Status,
                a.Tiers.Select(t => new TierResponseDto(
                    t.Id,
                    t.Tier,
                    t.Description,
                    string.Join(",", t.TierFeatures.Select(tf => tf.Feature!.FeatureKey)),
                    t.Parameters
                )).ToList(),
                a.Features.Select(f => new FeatureResponseDto(
                    f.Id,
                    f.FeatureKey,
                    f.DisplayName,
                    f.Category,
                    f.IsActive
                )).ToList()
            ));
        }

        public async Task<IEnumerable<AppEntity>> GetAllAsync()
        {
            return await _context.Applications
                .Include(a => a.Features)
                .Include(a => a.Tiers)
                    .ThenInclude(t => t.TierFeatures)
                        .ThenInclude(tf => tf.Feature)
                .ToListAsync();
        }

        public async Task<AppEntity?> GetByIdAsync(Guid id)
        {
            return await _context.Applications
                .Include(a => a.Features)
                .Include(a => a.Tiers)
                    .ThenInclude(t => t.TierFeatures)
                        .ThenInclude(tf => tf.Feature)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<AppEntity?> GetBySlugAsync(string slug)
        {
            return await _context.Applications
                .Include(a => a.Features)
                .Include(a => a.Tiers)
                    .ThenInclude(t => t.TierFeatures)
                        .ThenInclude(tf => tf.Feature)
                .FirstOrDefaultAsync(a => a.Slug == slug);
        }

        public async Task AddAsync(AppEntity application)
        {
            await _context.Applications.AddAsync(application);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(AppEntity application)
        {
            _context.Applications.Update(application);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(AppEntity application)
        {
            _context.Applications.Remove(application);
            await _context.SaveChangesAsync();
        }

        public async Task<ApplicationFeature?> GetFeatureByIdAsync(Guid featureId)
        {
            return await _context.ApplicationFeatures.FindAsync(featureId);
        }

        public async Task UpdateFeatureAsync(ApplicationFeature feature)
        {
            _context.ApplicationFeatures.Update(feature);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteFeatureAsync(ApplicationFeature feature)
        {
            _context.ApplicationFeatures.Remove(feature);
            await _context.SaveChangesAsync();
        }

        public async Task AddFeatureAsync(ApplicationFeature feature)
        {
            await _context.ApplicationFeatures.AddAsync(feature);
            await _context.SaveChangesAsync();
        }

        public async Task<ApplicationTier?> GetTierByIdAsync(Guid tierId)
        {
            return await _context.ApplicationTiers
                .Include(t => t.TierFeatures)
                .FirstOrDefaultAsync(t => t.Id == tierId);
        }

        public async Task AddTierAsync(ApplicationTier tier)
        {
            await _context.ApplicationTiers.AddAsync(tier);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTierAsync(ApplicationTier tier)
        {
            _context.ApplicationTiers.Update(tier);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTierAsync(ApplicationTier tier)
        {
            _context.ApplicationTiers.Remove(tier);
            await _context.SaveChangesAsync();
        }
    }
}
