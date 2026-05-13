using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Domain.Entities;
using BioLicense_Portal.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AppEntity = BioLicense_Portal.Domain.Entities.Application;
using BioLicense_Portal.Domain.Enums;

namespace BioLicense_Portal.Infrastructure.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly LicenseRepository _licenseRepository;
        private readonly ApplicationRepository _appRepository;
        private readonly ILicenseGeneratorService _licenseGenerator;
        private readonly IKeyGeneratorService _keyGenerator;
        private readonly IEmailService _emailService;

        public LicenseService(
            LicenseRepository licenseRepository, 
            ApplicationRepository appRepository,
            ILicenseGeneratorService licenseGenerator,
            IKeyGeneratorService keyGenerator,
            IEmailService emailService)
        {
            _licenseRepository = licenseRepository;
            _appRepository = appRepository;
            _licenseGenerator = licenseGenerator;
            _keyGenerator = keyGenerator;
            _emailService = emailService;
        }

        public async Task<LicenseRequestResponseDto> CreateRequestAsync(Guid distributorId, CreateLicenseRequestDto request)
        {
            var app = await _appRepository.GetByIdAsync(request.ApplicationId);
            if (app == null) throw new KeyNotFoundException("Application not found.");

            var (finalFeatures, finalParameters) = await ResolveTierConfigsAsync(app, request.LicenseTier, request.SelectedFeatures, request.Parameters);

            var licenseRequest = new LicenseRequest
            {
                Id = Guid.NewGuid(),
                ApplicationId = request.ApplicationId,
                RequesterUserId = distributorId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                MachineId = request.DeviceId,
                LicenseType = request.LicenseType,
                LicenseTier = request.LicenseTier,
                ExpiryDate = CalculateExpiryDate(request.LicenseType, request.ExpiryDate),
                Features = string.Join(",", finalFeatures),
                LicenseParameters = JsonSerializer.Serialize(finalParameters),
                RequestStatus = BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Pending,
                Notes = request.Notes,
                RequestedAt = DateTime.UtcNow
            };

            await _licenseRepository.AddRequestAsync(licenseRequest);
            
            // Reload to get includes
            var savedRequest = await _licenseRepository.GetRequestByIdAsync(licenseRequest.Id);
            return MapToDto(savedRequest!);
        }

        private DateTime CalculateExpiryDate(LicenseType type, DateTime? requestedDate)
        {
            if (requestedDate.HasValue) return requestedDate.Value;

            return type switch
            {
                LicenseType.Trial => DateTime.UtcNow.AddDays(7),
                LicenseType.Annual => DateTime.UtcNow.AddYears(1),
                LicenseType.Perpetual => DateTime.UtcNow.AddYears(99),
                _ => DateTime.UtcNow.AddYears(1)
            };
        }

        private class TierConfigItem
        {
            public List<string>? Features { get; set; }
            public Dictionary<string, object>? Parameters { get; set; }
        }

        public async Task<LicenseResponseDto> CreateLicenseDirectAsync(Guid ownerId, CreateLicenseRequestDto request)
        {
            var app = await _appRepository.GetByIdAsync(request.ApplicationId);
            if (app == null) throw new KeyNotFoundException("Application not found.");

            var (finalFeatures, finalParameters) = await ResolveTierConfigsAsync(app, request.LicenseTier, request.SelectedFeatures, request.Parameters);

            // 1. Pre-generate IDs to avoid double updates
            var requestId = Guid.NewGuid();
            var licenseId = Guid.NewGuid();

            var licenseRequest = new LicenseRequest
            {
                Id = requestId,
                ApplicationId = request.ApplicationId,
                RequesterUserId = request.RequesterUserId ?? ownerId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                MachineId = request.DeviceId,
                LicenseType = request.LicenseType,
                LicenseTier = request.LicenseTier,
                ExpiryDate = CalculateExpiryDate(request.LicenseType, request.ExpiryDate),
                Features = string.Join(",", finalFeatures),
                LicenseParameters = JsonSerializer.Serialize(finalParameters),
                RequestStatus = BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Approved,
                Notes = request.Notes,
                RequestedAt = DateTime.UtcNow,
                ApproverUserId = ownerId,
                ProcessedAt = DateTime.UtcNow,
                LicenseRecordId = licenseId // Link it upfront
            };

            await _licenseRepository.AddRequestAsync(licenseRequest);

            // 2. Generate and save license with pre-generated ID
            var licenseRecord = await GenerateAndRecordLicenseAsync(licenseRequest, app, ownerId, licenseId);
            
            // 3. Send Email (licenseRecord now has Application attached in-memory)
            await SendLicenseEmailAsync(licenseRecord); 

            return MapToLicenseDto(licenseRecord);
        }

        public async Task<IEnumerable<LicenseRequestResponseDto>> GetMyRequestsAsync(Guid distributorId)
        {
            var requests = await _licenseRepository.GetRequestsByDistributorAsync(distributorId);
            return requests.Select(MapToDto);
        }

        public async Task<IEnumerable<LicenseRequestResponseDto>> GetPendingRequestsAsync()
        {
            var requests = await _licenseRepository.GetPendingRequestsAsync();
            return requests.Select(MapToDto);
        }

        public async Task<bool> ApproveRequestAsync(Guid requestId, Guid engineerId)
        {
            var request = await _licenseRepository.GetRequestByIdAsync(requestId);
            if (request == null || request.RequestStatus != BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Pending) return false;

            var app = request.Application;
            if (app == null) throw new KeyNotFoundException("Application not found.");

            var licenseId = Guid.NewGuid();
            var licenseRecord = await GenerateAndRecordLicenseAsync(request, app, engineerId, licenseId);

            // Update request status
            request.RequestStatus = BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Completed;
            request.ApproverUserId = engineerId;
            request.ProcessedAt = DateTime.UtcNow;
            request.LicenseRecordId = licenseId;

            await _licenseRepository.UpdateRequestAsync(request);

            // Send Email
            await SendLicenseEmailAsync(licenseRecord);

            return true;
        }

        private async Task<LicenseRecord> GenerateAndRecordLicenseAsync(LicenseRequest request, AppEntity app, Guid actorId, Guid? preGeneratedId = null)
        {
            if (string.IsNullOrEmpty(app.PrivateKeyEncrypted))
                throw new InvalidOperationException("Application private key is missing.");

            // 1. Decrypt private key (Master Secret is handled internally by KeyGeneratorService)
            var privateKey = _keyGenerator.DecryptPrivateKey(app.PrivateKeyEncrypted);

            // 2. Generate license string using Standard.Licensing
            var licenseContent = _licenseGenerator.GenerateLicense(app, request, privateKey);

            // 3. Create LicenseRecord
            var licenseRecord = new LicenseRecord
            {
                Id = preGeneratedId ?? Guid.NewGuid(),
                Licenseid = Guid.NewGuid(),
                ApplicationId = app.Id,
                Application = app, // Attach in-memory to avoid reload
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                MachineId = request.MachineId,
                LicenseType = request.LicenseType,
                LicenseTier = request.LicenseTier,
                LicenseParameters = request.LicenseParameters,
                Features = request.Features,
                LicenseContent = licenseContent,
                IssuedAt = DateTime.UtcNow,
                ExpiredAt = request.ExpiryDate ?? DateTime.UtcNow.AddYears(1),
                GeneratedByUserId = actorId,
                AssignedToUserId = request.RequesterUserId != actorId ? request.RequesterUserId : null,
                Status = Domain.Enums.LicenseStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _licenseRepository.AddLicenseRecordAsync(licenseRecord);
            return licenseRecord;
        }

        private async Task<(List<string> Features, Dictionary<string, object> Parameters)> ResolveTierConfigsAsync(
            AppEntity app, 
            LicenseTier tier, 
            List<string>? selectedFeatures, 
            Dictionary<string, object>? parameters)
        {
            var finalFeatures = selectedFeatures ?? new List<string>();
            var finalParameters = parameters ?? new Dictionary<string, object>();

            if (tier != LicenseTier.Custom && app.Tiers != null)
            {
                var config = app.Tiers.FirstOrDefault(t => t.Tier == tier);
                if (config != null)
                {
                    if (config.TierFeatures != null && config.TierFeatures.Any())
                    {
                        finalFeatures = config.TierFeatures
                            .Where(tf => tf.Feature != null)
                            .Select(tf => tf.Feature!.FeatureKey)
                            .ToList();
                    }
                    if (!string.IsNullOrEmpty(config.Parameters))
                    {
                        try
                        {
                            var parsedParams = JsonSerializer.Deserialize<Dictionary<string, object>>(config.Parameters);
                            if (parsedParams != null) finalParameters = parsedParams;
                        }
                        catch { /* Ignore invalid JSON */ }
                    }
                }
            }

            return (finalFeatures, finalParameters);
        }

        private async Task SendLicenseEmailAsync(LicenseRecord license)
        {
            var appName = license.Application?.Name ?? "BioLicense App";
            var customerName = license.CustomerName;
            
            try
            {
                Console.WriteLine($"[Email] Starting delivery for License {license.Id}...");
                
                var subject = $"[BioLicense] License Issued for {appName} - {customerName}";
                
                // Read template from file
                var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "LicenseEmail.html");
                if (!File.Exists(templatePath))
                {
                    templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "LicenseEmail.html");
                }
                
                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"[Email] ERROR: Template not found at {templatePath}");
                    return;
                }

                var bodyContent = await File.ReadAllTextAsync(templatePath);

                // Perform replacements
                var baseBody = bodyContent.Replace("{{CustomerName}}", customerName)
                           .Replace("{{AppName}}", appName)
                           .Replace("{{LicenseTier}}", license.LicenseTier.ToString())
                           .Replace("{{LicenseType}}", license.LicenseType.ToString())
                           .Replace("{{ExpiryDate}}", license.ExpiredAt.ToString("yyyy-MM-dd"))
                           .Replace("{{MachineId}}", license.MachineId)
                           .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

                var fileName = $"{appName}_{customerName}.lic";
                var fileData = System.Text.Encoding.UTF8.GetBytes(license.LicenseContent ?? "");

                // 1. Send to Customer
                if (!string.IsNullOrEmpty(license.CustomerEmail))
                {
                    try {
                        await _emailService.SendEmailAsync(license.CustomerEmail!, subject, baseBody, fileName, fileData);
                        Console.WriteLine($"[Email] Success: Sent to Customer ({license.CustomerEmail})");
                    } catch (Exception ex) {
                        Console.WriteLine($"[Email] FAILED to Customer ({license.CustomerEmail}): {ex.Message}");
                    }
                }

                // 2. Send to Distributor if assigned
                if (license.AssignedToUserId.HasValue)
                {
                    try {
                        var distributor = await _licenseRepository.GetUserByIdAsync(license.AssignedToUserId.Value);
                        if (distributor != null && !string.IsNullOrEmpty(distributor.Email))
                        {
                            var distBody = baseBody.Replace($"Hi {customerName},", $"Hi {distributor.FullName},")
                                               .Replace("Your license", $"A license for your customer <strong>{customerName}</strong>");
                            
                            await _emailService.SendEmailAsync(distributor.Email, subject, distBody, fileName, fileData);
                            Console.WriteLine($"[Email] Success: Sent to Distributor ({distributor.Email})");
                        } else {
                            Console.WriteLine($"[Email] Skip Distributor: No email found for User ID {license.AssignedToUserId}");
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"[Email] FAILED to Distributor: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email] Global Failure: {ex.Message}");
            }
        }

        public async Task<bool> RejectRequestAsync(Guid requestId, Guid engineerId, string reason)
        {
            var request = await _licenseRepository.GetRequestByIdAsync(requestId);
            if (request == null || request.RequestStatus != BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Pending) return false;

            request.RequestStatus = BioLicense_Portal.Domain.Enums.LicenseRequestStatus.Rejected;
            request.RejectionReason = reason;
            request.ApproverUserId = engineerId;
            request.ProcessedAt = DateTime.UtcNow;

            await _licenseRepository.UpdateRequestAsync(request);
            return true;
        }

        public async Task<IEnumerable<LicenseResponseDto>> GetAllLicensesAsync()
        {
            var licenses = await _licenseRepository.GetAllLicensesAsync();
            return licenses.Select(MapToLicenseDto);
        }

        public async Task<LicenseResponseDto?> GetLicenseByIdAsync(Guid licenseId)
        {
            var license = await _licenseRepository.GetLicenseByIdAsync(licenseId);
            return license != null ? MapToLicenseDto(license) : null;
        }

        public async Task<string?> GetLicenseContentAsync(Guid licenseId)
        {
            var license = await _licenseRepository.GetLicenseByIdAsync(licenseId);
            return license?.LicenseContent;
        }

        public async Task<bool> RevokeLicenseAsync(Guid licenseId, string reason)
        {
            var license = await _licenseRepository.GetLicenseByIdAsync(licenseId);
            if (license == null || license.Status != Domain.Enums.LicenseStatus.Active) return false;

            license.Status = Domain.Enums.LicenseStatus.Revoked;
            license.RevokedAt = DateTime.UtcNow;
            license.RevokedReason = reason;

            await _licenseRepository.UpdateLicenseRecordAsync(license);
            return true;
        }

        private LicenseRequestResponseDto MapToDto(LicenseRequest request)
        {
            return new LicenseRequestResponseDto(
                request.Id,
                request.Application?.Name ?? "Unknown",
                request.Requester?.Username ?? "Unknown",
                request.CustomerName,
                request.MachineId,
                (int)request.RequestStatus,
                request.RequestStatus.ToString(),
                request.Features ?? "",
                request.LicenseParameters,
                request.RequestedAt,
                request.ExpiryDate,
                request.Notes,
                request.RejectionReason
            );
        }

        private LicenseResponseDto MapToLicenseDto(LicenseRecord license)
        {
            return new LicenseResponseDto(
                license.Id,
                license.Application?.Name ?? "Unknown",
                license.CustomerName,
                license.MachineId,
                license.LicenseType,
                license.LicenseTier,
                license.Features,
                license.LicenseParameters,
                license.IssuedAt,
                license.ExpiredAt,
                license.Status.ToString()
            );
        }
    }
}
