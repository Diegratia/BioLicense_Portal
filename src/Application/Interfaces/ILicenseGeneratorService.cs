using BioLicense_Portal.Domain.Entities;
using BioLicense_Portal.Domain.Enums;
using System;
using System.Collections.Generic;

namespace BioLicense_Portal.Application.Interfaces
{
    public interface ILicenseGeneratorService
    {
        string GenerateLicense(
            Domain.Entities.Application app, 
            string customerName,
            string machineId,
            LicenseType type,
            LicenseTier tier,
            DateTime expiryDate,
            List<string> features,
            Dictionary<string, object> parameters,
            string privateKey);
    }
}
