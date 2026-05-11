using BioLicense_Portal.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLicense_Portal.Domain.Entities
{
    public class LicenseRequest : BaseEntity
    {
        [Column("application_id")]
        public Guid ApplicationId { get; set; }
        [Column("requester_user_id")]
        public Guid RequesterUserId { get; set; } // Distributor Id
        [Column("customer_name")]
        public string CustomerName { get; set; } = string.Empty;
        [Column("customer_email")]
        public string? CustomerEmail { get; set; }
        [Column("machine_id")]
        public string MachineId { get; set; } = string.Empty;
        [Column("license_type")]
        public LicenseType LicenseType { get; set; }
        [Column("license_tier")]
        public LicenseTier LicenseTier { get; set; }
        [Column("license_parameters")]
        public string? LicenseParameters { get; set; } // custom feature parameter dalam json, contoh pada ble yaitu max reader dan max beacon
        [Column("features")]
        public string? Features { get; set; } // string comma feature key
        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }
        [Column("request_status")]
        public LicenseRequestStatus RequestStatus { get; set; } = LicenseRequestStatus.Pending; // Pending, Approved, Rejected, Completed
        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }
        
        [Column("approver_user_id")]
        public Guid? ApproverUserId { get; set; } // Owner / Engineer
        [Column("license_record_id")]
        public Guid? LicenseRecordId { get; set; } // Linked license after generation
        [Column("notes")]
        public string? Notes { get; set; }
        [Column("requested_at")]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }
        public Application? Application { get; set; }
        public User? Requester { get; set; }
        public User? Approver { get; set; }
        public LicenseRecord? LicenseRecord { get; set; }
    }
}
