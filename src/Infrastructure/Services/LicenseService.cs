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
using BioLicense_Portal.Application.Exceptions;
using BioLicense_Portal.Domain.Enums;

namespace BioLicense_Portal.Infrastructure.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly LicenseRepository _licenseRepository;
        private readonly ApplicationRepository _appRepository;
        private readonly UserRepository _userRepository;
        private readonly ILicenseGeneratorService _licenseGenerator;
        private readonly IKeyGeneratorService _keyGenerator;
        private readonly IEmailService _emailService;

        public LicenseService(
            LicenseRepository licenseRepository,
            ApplicationRepository appRepository,
            UserRepository userRepository,
            ILicenseGeneratorService licenseGenerator,
            IKeyGeneratorService keyGenerator,
            IEmailService emailService)
        {
            _licenseRepository = licenseRepository;
            _appRepository = appRepository;
            _userRepository = userRepository;
            _licenseGenerator = licenseGenerator;
            _keyGenerator = keyGenerator;
            _emailService = emailService;
        }

        public async Task<LicenseRequestResponseDto> CreateRequestAsync(Guid distributorId, CreateLicenseRequestDto request)
        {
            var app = await _appRepository.GetByIdAsync(request.ApplicationId);
            if (app == null) throw new KeyNotFoundException("Application not found.");

            ValidateTierRequest(request.LicenseTier, app, request.SelectedFeatureIds, request.Parameters);
            var (finalFeatures, finalParameters) = ResolveTierConfigs(app, request.LicenseTier, request.SelectedFeatureIds, request.Parameters);

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

        public async Task<LicenseResponseDto> CreateLicenseDirectAsync(Guid ownerId, CreateLicenseRequestDto request)
        {
            var app = await _appRepository.GetByIdAsync(request.ApplicationId);
            if (app == null) throw new KeyNotFoundException("Application not found.");

            ValidateTierRequest(request.LicenseTier, app, request.SelectedFeatureIds, request.Parameters);
            var (finalFeatures, finalParameters) = ResolveTierConfigs(app, request.LicenseTier, request.SelectedFeatureIds, request.Parameters);

            // 1. Pre-generate IDs to avoid double updates
            var requestId = Guid.NewGuid();
            var licenseId = Guid.NewGuid();

            // 1. Directly Generate and save the LicenseRecord
            var licenseRecord = await GenerateLicenseInternalAsync(
                app,
                request.CustomerName,
                request.CustomerEmail,
                request.DeviceId,
                request.LicenseType,
                request.LicenseTier,
                CalculateExpiryDate(request.LicenseType, request.ExpiryDate),
                finalFeatures,
                finalParameters,
                licenseId);

            // 2. Send Email
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
            
            // Parse parameters from request
            var parameters = !string.IsNullOrEmpty(request.LicenseParameters) 
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(request.LicenseParameters) 
                : new Dictionary<string, object>();
                
            var featuresList = request.Features?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();

            var licenseRecord = await GenerateLicenseInternalAsync(
                app,
                request.CustomerName,
                request.CustomerEmail,
                request.MachineId,
                request.LicenseType,
                request.LicenseTier,
                request.ExpiryDate ?? DateTime.UtcNow.AddYears(1),
                featuresList,
                parameters ?? new Dictionary<string, object>(),
                licenseId);

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

        private async Task<LicenseRecord> GenerateLicenseInternalAsync(
            AppEntity app,
            string customerName,
            string? customerEmail,
            string machineId,
            LicenseType type,
            LicenseTier tier,
            DateTime expiryDate,
            List<string> features,
            Dictionary<string, object> parameters,
            Guid? preGeneratedId = null)
        {
            if (string.IsNullOrEmpty(app.PrivateKeyEncrypted))
                throw new InvalidOperationException("Application private key is missing.");

            // 1. Decrypt private key
            var privateKey = _keyGenerator.DecryptPrivateKey(app.PrivateKeyEncrypted);

            // 2. Generate license string
            var licenseContent = _licenseGenerator.GenerateLicense(
                app, 
                customerName, 
                machineId, 
                type, 
                tier, 
                expiryDate, 
                features, 
                parameters, 
                privateKey);

            // 3. Create LicenseRecord
            var licenseRecord = new LicenseRecord
            {
                Id = preGeneratedId ?? Guid.NewGuid(),
                Licenseid = Guid.NewGuid(),
                ApplicationId = app.Id,
                Application = app,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                MachineId = machineId,
                LicenseType = type,
                LicenseTier = tier,
                LicenseParameters = JsonSerializer.Serialize(parameters),
                Features = string.Join(",", features),
                LicenseContent = licenseContent,
                IssuedAt = DateTime.UtcNow,
                ExpiredAt = expiryDate
            };

            await _licenseRepository.AddLicenseRecordAsync(licenseRecord);
            return licenseRecord;
        }

        private void ValidateTierRequest(LicenseTier tier, AppEntity app, List<Guid>? selectedFeatureIds, Dictionary<string, object>? parameters)
        {
            if (tier == LicenseTier.Custom)
            {
                // Custom tier wajib isi sendiri
                if (selectedFeatureIds == null || selectedFeatureIds.Count == 0)
                    throw new BusinessException("Custom tier requires SelectedFeatureIds.");

                // Wajib ambil dari list feature application tersebut
                foreach (var fId in selectedFeatureIds)
                {
                    if (!app.Features.Any(f => f.Id == fId))
                    {
                        throw new BusinessException($"Feature ID '{fId}' is not registered for application '{app.Name}'.");
                    }
                }
            }
            else
            {
                // Non-Custom tier: harus ada di config
                var config = app.Tiers?.FirstOrDefault(t => t.Tier == tier);
                if (config == null)
                    throw new BusinessException($"Tier {tier} is not configured for application '{app.Name}'.");
            }
        }

        private (List<string> Features, Dictionary<string, object> Parameters) ResolveTierConfigs(
            AppEntity app,
            LicenseTier tier,
            List<Guid>? selectedFeatureIds,
            Dictionary<string, object>? parameters)
        {
            // Non-Custom: 100% dari tier config, user input diabaikan
            if (tier != LicenseTier.Custom && app.Tiers != null)
            {
                var config = app.Tiers.FirstOrDefault(t => t.Tier == tier);
                if (config == null)
                    return (new List<string>(), new Dictionary<string, object>());

                var features = (config.TierFeatures != null && config.TierFeatures.Any())
                    ? config.TierFeatures
                        .Where(tf => tf.Feature != null)
                        .Select(tf => $"{tf.Feature!.Category.ToString().ToLowerInvariant()}.{tf.Feature!.FeatureKey}")
                        .ToList()
                    : new List<string>();

                var parms = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(config.Parameters))
                {
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(config.Parameters);
                        if (parsed != null) parms = parsed;
                    }
                    catch { /* ignore */ }
                }

                return (features, parms);
            }

            // Custom: dari user input
            var customFeatures = new List<string>();
            if (selectedFeatureIds != null)
            {
                foreach (var fId in selectedFeatureIds)
                {
                    // Pasti ada karena sudah divalidasi di ValidateTierRequest
                    var feature = app.Features.First(f => f.Id == fId);
                    customFeatures.Add($"{feature.Category.ToString().ToLowerInvariant()}.{feature.FeatureKey}");
                }
            }

            return (customFeatures, parameters ?? new Dictionary<string, object>());
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
                        var distributor = await _userRepository.GetByIdAsync(license.AssignedToUserId.Value);
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
