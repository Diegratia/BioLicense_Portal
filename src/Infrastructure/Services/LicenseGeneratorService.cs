using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Domain.Entities;
using DomainEnums = BioLicense_Portal.Domain.Enums;
using Standard.Licensing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BioLicense_Portal.Infrastructure.Services
{
    public class LicenseGeneratorService : ILicenseGeneratorService
    {
        public string GenerateLicense(
            Domain.Entities.Application app, 
            string customerName,
            string machineId,
            DomainEnums.LicenseType type,
            DomainEnums.LicenseTier tier,
            DateTime expiryDate,
            List<string> features,
            Dictionary<string, object> parameters,
            string privateKey)
        {
            var licenseType = type == DomainEnums.LicenseType.Trial 
                ? Standard.Licensing.LicenseType.Trial 
                : Standard.Licensing.LicenseType.Standard;

            var builder = License.New()
                .WithUniqueIdentifier(Guid.NewGuid())
                .As(licenseType)
                .ExpiresAt(expiryDate)
                .WithAdditionalAttributes(new Dictionary<string, string>
                {
                    { "MachineID", machineId },
                    { "LicenseTier", tier.ToString() },
                    { "LicenseType", type.ToString() },
                    { "Features", string.Join(",", features) },
                    { "ApplicationSlug", app.Slug }
                })
                .LicensedTo(customerName, "");

            // Add custom parameters to attributes
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    builder.WithAdditionalAttributes(new Dictionary<string, string> { { param.Key, param.Value?.ToString() ?? "" } });
                }
            }

            var license = builder.CreateAndSignWithPrivateKey(privateKey, string.Empty);
            return license.ToString();
        }
    }
}
