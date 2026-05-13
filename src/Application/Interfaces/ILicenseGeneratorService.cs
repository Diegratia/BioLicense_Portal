using BioLicense_Portal.Domain.Entities;
using System.Collections.Generic;

namespace BioLicense_Portal.Application.Interfaces
{
    public interface ILicenseGeneratorService
    {
        string GenerateLicense(BioLicense_Portal.Domain.Entities.Application app, LicenseRequest request, string privateKey);
    }
}
