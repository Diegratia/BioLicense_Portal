using BioLicense_Portal.Infrastructure.Data;
using BioLicense_Portal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioLicense_Portal.Infrastructure.Repositories
{
    public class LicenseRepository
    {
        private readonly BioLicenseDbContext _context;

        public LicenseRepository(BioLicenseDbContext context)
        {
            _context = context;
        }

        public async Task<LicenseRequest?> GetRequestByIdAsync(Guid id)
        {
            return await _context.LicenseRequests
                .Include(r => r.Application)
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<LicenseRequest>> GetRequestsByDistributorAsync(Guid distributorId)
        {
            return await _context.LicenseRequests
                .Include(r => r.Application)
                .Where(r => r.RequesterUserId == distributorId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<LicenseRequest>> GetPendingRequestsAsync()
        {
            return await _context.LicenseRequests
                .Include(r => r.Application)
                .Include(r => r.Requester)
                .Where(r => r.RequestStatus == Domain.Enums.LicenseRequestStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task AddRequestAsync(LicenseRequest request)
        {
            await _context.LicenseRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRequestAsync(LicenseRequest request)
        {
            _context.LicenseRequests.Update(request);
            await _context.SaveChangesAsync();
        }
    }
}
