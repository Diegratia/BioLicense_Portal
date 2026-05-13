using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Domain.Entities;
using Standard.Licensing;
using Standard.Licensing.Validation;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace BioLicense_Portal.Infrastructure.Services
{
    public class LicenseGeneratorService : ILicenseGeneratorService
    {
        public string GenerateLicense(BioLicense_Portal.Domain.Entities.Application app, LicenseRequest request, string privateKey)
        {
            var licenseType = request.LicenseType == Domain.Enums.LicenseType.Trial 
                ? Standard.Licensing.LicenseType.Trial 
                : Standard.Licensing.LicenseType.Standard;

            var builder = Standard.Licensing.License.New()
                .WithUniqueIdentifier(Guid.NewGuid())
                .As(licenseType)
                .ExpiresAt(request.ExpiryDate ?? DateTime.UtcNow.AddYears(1))
                .WithAdditionalAttributes(ParseParameters(request.LicenseParameters))
                .WithAdditionalAttributes(new Dictionary<string, string>
                {
                    { "MachineID", request.MachineId },
                    { "LicenseTier", request.LicenseTier.ToString() },
                    { "Features", request.Features ?? "" },
                    { "ApplicationSlug", app.Slug }
                })
                .LicensedTo(request.CustomerName, request.CustomerEmail ?? "");
            
            // Gunakan string.Empty daripada null untuk menghindari NullReferenceException di dalam library
            var license = builder.CreateAndSignWithPrivateKey(privateKey, string.Empty);

            return license.ToString();
        }

        private Dictionary<string, string> ParseParameters(string? json)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json)) return result;

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        result.Add(kvp.Key, kvp.Value?.ToString() ?? "");
                    }
                }
            }
            catch
            {
                // Ignore invalid JSON
            }

            return result;
        }
    }
}
