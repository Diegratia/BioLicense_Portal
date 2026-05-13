using System.ComponentModel.DataAnnotations.Schema;
using BioLicense_Portal.Domain.Enums;

namespace BioLicense_Portal.Domain.Entities
{
    public class LicenseRecord : BaseEntity
    {
        [Column("license_id")]
        public Guid Licenseid { get; set; } = Guid.NewGuid();
        [Column("application_id")]
        public Guid ApplicationId { get; set; }
        [Column("customer_name")]
        public string CustomerName { get; set; } = string.Empty;
        [Column("customer_email")]
        public string? CustomerEmail { get; set; }
        [Column("machine_id")]
        public string MachineId { get; set; } = string.Empty;
        [Column("license_type")]
        public LicenseType LicenseType { get; set; }
        [Column("license_tier")]
        public LicenseTier LicenseTier { get; set; } = LicenseTier.Core;
        [Column("license_parameters")]
        public string? LicenseParameters { get; set; } // JSON string for any additional parameters
        [Column("features")]
        public string? Features { get; set; } // Comma-separated feature keys
        [Column("custom_attributes")]
        public string? CustomAttributes { get; set; } // JSON string
        [Column("license_content")]
        public string? LicenseContent { get; set; } // Signed .lic content
        [Column("issued_at")]
        public DateTime IssuedAt { get; set; }
        [Column("expired_at")]  
        public DateTime ExpiredAt { get; set; }
        [Column("status")]
        public LicenseStatus Status { get; set; } = LicenseStatus.Active;
        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }
        [Column("revoked_reason")]
        public string? RevokedReason { get; set; }
        [Column("generated_by_user_id")]
        public Guid GeneratedByUserId { get; set; }
        [Column("assigned_to_user_id")]
        public Guid? AssignedToUserId { get; set; }

        public Application? Application { get; set; }
        public User? AssignedTo { get; set; }
        public User? GeneratedBy { get; set; }
    }
}
